/* Independent diagnostic only: preserve one relative fault location, never a register or memory dump. */
#define _GNU_SOURCE
#include <errno.h>
#include <fcntl.h>
#include <link.h>
#include <signal.h>
#include <stdatomic.h>
#include <stdint.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/syscall.h>
#include <time.h>
#include <ucontext.h>
#include <unistd.h>

struct executable_range {
    uintptr_t start, end, base;
    unsigned char module;
};

static struct executable_range ranges[32];
static unsigned int range_count;
static struct sigaction previous_action;
static volatile sig_atomic_t installed;
static atomic_uint capture_count;
static int record_fd = -1;
static unsigned char record[72];

/* Fixed-width little-endian values make the record independent of native struct padding. */
static void put32(unsigned char *buffer, unsigned int at, uint32_t value)
{
    for (unsigned int i = 0; i < 4; ++i) buffer[at + i] = (unsigned char)(value >> (i * 8));
}

static void put64(unsigned char *buffer, unsigned int at, uint64_t value)
{
    for (unsigned int i = 0; i < 8; ++i) buffer[at + i] = (unsigned char)(value >> (i * 8));
}

/* Resolve only reviewed engine executable ranges before entering any signal context. */
static int inspect_module(struct dl_phdr_info *info, size_t size, void *data)
{
    (void)size;
    (void)data;
    const char *name = strrchr(info->dlpi_name, '/');
    name = name == NULL ? info->dlpi_name : name + 1;
    unsigned char module = 0;
    if (strcmp(name, "libmonosgen-2.0.so") == 0) module = 1;
    else if (strcmp(name, "libmonodroid.so") == 0) module = 2;
    else if (strcmp(name, "libsqlcipher.so") == 0) module = 3;
#if defined(ZANANCE_FAULT_PC_SELFTEST)
    else if (strcmp(name, "libzanance_fault_origin.so") == 0) module = 4;
#endif
    if (module == 0) return 0;
    for (ElfW(Half) i = 0; i < info->dlpi_phnum; ++i) {
        const ElfW(Phdr) *p = &info->dlpi_phdr[i];
        if (p->p_type != PT_LOAD || (p->p_flags & PF_X) == 0) continue;
        uintptr_t base = (uintptr_t)info->dlpi_addr;
        if (p->p_vaddr > UINTPTR_MAX - base) return 1;
        uintptr_t start = base + (uintptr_t)p->p_vaddr;
        if (p->p_memsz > UINTPTR_MAX - start || range_count == 32) return 1;
        ranges[range_count++] = (struct executable_range){ start, start + (uintptr_t)p->p_memsz, base, module };
    }
    return 0;
}

/* No unwinding, allocation, loader lookup, formatting, managed callback or raw address output here. */
static void observe_fault(int signal_number, siginfo_t *info, void *context)
{
    int saved_errno = errno;
    if (signal_number == SIGSEGV && info != NULL && context != NULL && info->si_code > 0) {
        const ucontext_t *uc = (const ucontext_t *)context;
#if defined(__aarch64__)
        uintptr_t pc = (uintptr_t)uc->uc_mcontext.pc;
#elif defined(__x86_64__)
        uintptr_t pc = (uintptr_t)uc->uc_mcontext.gregs[REG_RIP];
#else
#error Only the qualified ARM64 and x64 diagnostic architectures are supported.
#endif
        unsigned char module = 0;
        uint64_t relative_pc = 0;
        /* Unmatched locations are passed through without recording an absolute PC or consuming capacity. */
        for (unsigned int i = 0; i < range_count; ++i) {
            if (pc >= ranges[i].start && pc < ranges[i].end) {
                module = ranges[i].module;
                relative_pc = (uint64_t)(pc - ranges[i].base);
                break;
            }
        }
        if (module != 0 && atomic_load_explicit(&capture_count, memory_order_relaxed) < 32) {
            unsigned int sequence = atomic_fetch_add_explicit(&capture_count, 1, memory_order_relaxed);
            if (sequence < 32) {
                unsigned char packet[72];
                memcpy(packet, record, sizeof(packet));
                packet[10] = (unsigned char)signal_number;
                packet[11] = module;
                put32(packet, 20, (uint32_t)gettid());
                put32(packet, 24, (uint32_t)info->si_code);
                put32(packet, 28, sequence + 1);
                put64(packet, 32, relative_pc);
                struct timespec timestamp;
                if (clock_gettime(CLOCK_MONOTONIC, &timestamp) == 0) {
                    put64(packet, 40, (uint64_t)timestamp.tv_sec);
                    put64(packet, 48, (uint64_t)timestamp.tv_nsec);
                }
                /* O_APPEND assigns each complete bounded write its own offset across faulting threads. */
                (void)write(record_fd, packet, sizeof(packet));
            }
        }
    }
    /* Runtime null checks can legitimately handle SIGSEGV. Keep their exact context and original handler;
       a captured location is not accepted as a fatal crash without independent process-death evidence. */
    if (previous_action.sa_handler != SIG_DFL) {
        errno = saved_errno;
        if ((previous_action.sa_flags & SA_SIGINFO) != 0)
            previous_action.sa_sigaction(signal_number, info, context);
        else
            previous_action.sa_handler(signal_number);
    } else {
        (void)sigaction(SIGSEGV, &previous_action, NULL);
        installed = 0;
        /* A hardware fault repeats at its original instruction; a generated signal needs explicit redelivery. */
        if (info == NULL || info->si_code <= 0)
            (void)syscall(SYS_tgkill, getpid(), gettid(), SIGSEGV);
    }
    errno = saved_errno;
}

/** Installs once for the exact current UID and a new0600 owned record, after module/identity preflight. */
__attribute__((visibility("default"))) int zanance_fault_pc_install(int fd, uint32_t expected_uid,
    const unsigned char token[16]);
int zanance_fault_pc_install(int fd, uint32_t expected_uid, const unsigned char token[16])
{
    struct stat file;
    if (installed || !atomic_is_lock_free(&capture_count) || atomic_load_explicit(&capture_count, memory_order_relaxed) != 0
        || token == NULL || getuid() != expected_uid || fstat(fd, &file) != 0
        || !S_ISREG(file.st_mode) || file.st_uid != expected_uid || file.st_size != 0
        || (file.st_mode & 0777) != 0600 || (fcntl(fd, F_GETFL) & O_APPEND) == 0) return -1;
    range_count = 0;
    if (dl_iterate_phdr(inspect_module, NULL) != 0 || range_count == 0) return -2;
    int duplicate = fcntl(fd, F_DUPFD_CLOEXEC, 0);
    if (duplicate < 0) return -3;
    if (sigaction(SIGSEGV, NULL, &previous_action) != 0) { close(duplicate); return -4; }
    if (previous_action.sa_handler == SIG_IGN
        || ((unsigned int)previous_action.sa_flags & ((unsigned int)SA_RESETHAND | (unsigned int)SA_NODEFER)) != 0) {
        close(duplicate); return -6;
    }
    memset(record, 0, sizeof(record));
    memcpy(record, "ZNCPC001", 8);
    record[8] = 1;
#if defined(__aarch64__)
    record[9] = 1;
#else
    record[9] = 2;
#endif
    put32(record, 12, expected_uid);
    put32(record, 16, (uint32_t)getpid());
    memcpy(record + 56, token, 16);
    record_fd = duplicate;
    struct sigaction action = previous_action;
    action.sa_sigaction = observe_fault;
    action.sa_flags = previous_action.sa_flags | SA_SIGINFO;
    if (sigaction(SIGSEGV, &action, NULL) != 0) { close(duplicate); record_fd = -1; return -5; }
    installed = 1;
    return 0;
}

/** Opens only a new private observation, retaining no managed pointer after the installation returns. */
__attribute__((visibility("default"))) int zanance_fault_pc_open_and_install(const char *path,
    uint32_t expected_uid, const unsigned char token[16]);
int zanance_fault_pc_open_and_install(const char *path, uint32_t expected_uid, const unsigned char token[16])
{
    if (path == NULL || token == NULL || getuid() != expected_uid) return -101;
    int fd = open(path, O_CREAT | O_EXCL | O_WRONLY | O_APPEND | O_CLOEXEC | O_NOFOLLOW, 0600);
    if (fd < 0) return -100;
    int result = zanance_fault_pc_install(fd, expected_uid, token);
    close(fd); /* The observer owns its independent close-on-exec descriptor after a successful arm. */
    return result;
}
