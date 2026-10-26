/* Fresh standalone native processes qualify signal chaining without opening an app or financial source. */
#define _GNU_SOURCE
#include <fcntl.h>
#include <setjmp.h>
#include <pthread.h>
#include <signal.h>
#include <stdatomic.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <ucontext.h>
#include <unistd.h>

extern int zanance_fault_pc_install(int fd, uint32_t uid, const unsigned char token[16]);
extern void zanance_fault_origin(void *page);
extern void zanance_fault_origin_recoverable(void *page);
extern unsigned char zanance_fault_origin_instruction[];

static sigjmp_buf recovery;
static volatile sig_atomic_t recovered;
static atomic_uint concurrently_recovered;

static void recover_concurrent_fault(int signal_number, siginfo_t *info, void *context)
{
    if (signal_number != SIGSEGV || info == NULL || context == NULL || info->si_code <= 0) _exit(92);
    ucontext_t *uc = (ucontext_t *)context;
#if defined(__aarch64__)
    if ((uintptr_t)uc->uc_mcontext.pc != (uintptr_t)zanance_fault_origin_instruction) _exit(93);
    uc->uc_mcontext.pc += 4;
#else
    if ((uintptr_t)uc->uc_mcontext.gregs[REG_RIP] != (uintptr_t)zanance_fault_origin_instruction) _exit(93);
    uc->uc_mcontext.gregs[REG_RIP] += 3;
#endif
    (void)atomic_fetch_add_explicit(&concurrently_recovered, 1, memory_order_relaxed);
}

static void *concurrent_worker(void *page)
{
    for (unsigned int i = 0; i < 5; ++i) zanance_fault_origin_recoverable(page);
    return NULL;
}

/* The original handler can recover; the observer must neither suppress it nor call it twice. */
static void recover_fault(int signal_number, siginfo_t *info, void *context)
{
    (void)context;
    if (signal_number != SIGSEGV || info == NULL) _exit(91);
    recovered++;
    siglongjmp(recovery, 1);
}

static int same_action(const struct sigaction *expected, const struct sigaction *actual)
{
    if (expected->sa_flags != actual->sa_flags || expected->sa_handler != actual->sa_handler) return 0;
    for (int signal_number = 1; signal_number < NSIG; ++signal_number)
        if (sigismember(&expected->sa_mask, signal_number) != sigismember(&actual->sa_mask, signal_number)) return 0;
    return 1;
}

int main(int argc, char **argv)
{
    if (argc != 3 || (strcmp(argv[1], "recover") != 0 && strcmp(argv[1], "fatal") != 0
        && strcmp(argv[1], "refuse") != 0 && strcmp(argv[1], "concurrent") != 0)) return 2;
    printf("{\"Phase\":\"process-started\",\"Mode\":\"%s\",\"Pid\":%ld,\"Uid\":%lu}\n",
        argv[1], (long)getpid(), (unsigned long)getuid());
    fflush(stdout);
    int fd = open(argv[2], O_CREAT | O_EXCL | O_WRONLY | O_APPEND | O_CLOEXEC | O_NOFOLLOW, 0600);
    if (fd < 0) return 3;
    struct sigaction original, action, after;
    if (sigaction(SIGSEGV, NULL, &original) != 0) return 4;
    memset(&action, 0, sizeof(action));
    sigemptyset(&action.sa_mask);
    action.sa_sigaction = recover_fault;
    action.sa_flags = SA_SIGINFO;
    const unsigned char token[16] = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15};
    void *page = mmap(NULL, (size_t)getpagesize(), PROT_NONE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
    if (page == MAP_FAILED) return 5;

    if (strcmp(argv[1], "refuse") == 0) {
        if (zanance_fault_pc_install(fd, (uint32_t)getuid() + 1, token) != -1) return 11;
        int pipe_fd[2];
        if (pipe(pipe_fd) != 0) return 12;
        if (zanance_fault_pc_install(pipe_fd[1], (uint32_t)getuid(), token) != -1) return 13;
        close(pipe_fd[0]); close(pipe_fd[1]);
        if (write(fd, "x", 1) != 1 || zanance_fault_pc_install(fd, (uint32_t)getuid(), token) != -1) return 14;
        if (ftruncate(fd, 0) != 0) return 15;
        action.sa_flags = (int)((unsigned int)SA_SIGINFO | (unsigned int)SA_RESETHAND);
        if (sigaction(SIGSEGV, &action, NULL) != 0) return 16;
        struct sigaction before_refusal;
        if (sigaction(SIGSEGV, NULL, &before_refusal) != 0) return 16;
        if (zanance_fault_pc_install(fd, (uint32_t)getuid(), token) != -6) return 17;
        if (sigaction(SIGSEGV, NULL, &after) != 0 || !same_action(&before_refusal, &after)) return 18;
        if (sigaction(SIGSEGV, &original, NULL) != 0) return 19;
        puts("{\"Mode\":\"refuse\",\"WrongUidPipeNonemptyAndResetHandlerRefused\":true,\"NoFinancialOrAppAccess\":true}");
        close(fd);
        return 0;
    }

    if (strcmp(argv[1], "concurrent") == 0) {
        if (!atomic_is_lock_free(&concurrently_recovered)) return 41;
        action.sa_sigaction = recover_concurrent_fault;
        if (sigaction(SIGSEGV, &action, NULL) != 0) return 42;
        if (zanance_fault_pc_install(fd, (uint32_t)getuid(), token) != 0) return 43;
        pthread_t threads[8];
        for (unsigned int i = 0; i < 8; ++i)
            if (pthread_create(&threads[i], NULL, concurrent_worker, page) != 0) return 44;
        for (unsigned int i = 0; i < 8; ++i)
            if (pthread_join(threads[i], NULL) != 0) return 45;
        struct stat file;
        if (atomic_load_explicit(&concurrently_recovered, memory_order_relaxed) != 40
            || fstat(fd, &file) != 0 || file.st_size != 32 * 72) return 46;
        if (sigaction(SIGSEGV, &original, NULL) != 0 || sigaction(SIGSEGV, NULL, &after) != 0
            || !same_action(&original, &after)) return 47;
        printf("{\"Mode\":\"concurrent\",\"OriginalHandlerCalls\":40,\"CappedRecords\":32,\"Threads\":8,\"OriginalDispositionRestored\":true,\"Pid\":%ld,\"Uid\":%lu}\n",
            (long)getpid(), (unsigned long)getuid());
        munmap(page, (size_t)getpagesize()); close(fd);
        return 0;
    }

    if (strcmp(argv[1], "fatal") == 0) {
        action.sa_handler = SIG_DFL;
        action.sa_flags = 0;
        if (sigaction(SIGSEGV, &action, NULL) != 0) return 21;
        if (zanance_fault_pc_install(fd, (uint32_t)getuid(), token) != 0) return 22;
        printf("{\"Mode\":\"fatal\",\"ArmedPid\":%ld,\"Uid\":%lu,\"NoFinancialOrAppAccess\":true}\n",
            (long)getpid(), (unsigned long)getuid());
        fflush(stdout);
        zanance_fault_origin(page);
        return 23; /* A fatal original signal must never reach this return. */
    }

    if (sigaction(SIGSEGV, &action, NULL) != 0) return 31;
    if (zanance_fault_pc_install(fd, (uint32_t)getuid(), token) != 0) return 32;
    /* A generated signal and an unmatched executable fault must preserve recovery without consuming records. */
    if (sigsetjmp(recovery, 1) == 0) raise(SIGSEGV);
    if (sigsetjmp(recovery, 1) == 0) *(volatile unsigned char *)page = 1;
    if (sigsetjmp(recovery, 1) == 0) zanance_fault_origin(page);
    if (sigsetjmp(recovery, 1) == 0) zanance_fault_origin(page);
    struct stat file;
    if (recovered != 4 || fstat(fd, &file) != 0 || file.st_size != 144) return 33;
    if (sigaction(SIGSEGV, &original, NULL) != 0 || sigaction(SIGSEGV, NULL, &after) != 0
        || !same_action(&original, &after)) return 34;
    printf("{\"Mode\":\"recover\",\"OriginalHandlerCalls\":4,\"OnlyMatchedRecords\":2,\"OriginalDispositionRestored\":true,\"Pid\":%ld,\"Uid\":%lu}\n",
        (long)getpid(), (unsigned long)getuid());
    munmap(page, (size_t)getpagesize());
    close(fd);
    return 0;
}
