/* D-574: conservative own-package command-to-ready clock, independent of PID-zero SDK records.
 * No app data, process memory, system setting or financial source is read or changed. */
#include <errno.h>
#include <fcntl.h>
#include <inttypes.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/wait.h>
#include <time.h>
#include <unistd.h>

static int identifier(const char *value) {
    if (strlen(value) != 32) return 0;
    for (int i = 0; i < 32; ++i)
        if (!((value[i] >= '0' && value[i] <= '9') || (value[i] >= 'a' && value[i] <= 'f'))) return 0;
    return 1;
}

static int64_t nanos(clockid_t clock) {
    struct timespec value;
    if (clock_gettime(clock, &value) != 0) return -1;
    return (int64_t)value.tv_sec * INT64_C(1000000000) + value.tv_nsec;
}

int main(int argc, char **argv) {
    if (getuid() != 2000) return 10;
    char boot[64] = {0};
    FILE *file = fopen("/proc/sys/kernel/random/boot_id", "r");
    if (!file || !fgets(boot, sizeof(boot), file)) return 11;
    fclose(file); boot[strcspn(boot, "\r\n")] = 0;
    if (strlen(boot) != 36) return 12;
    if (argc == 2 && strcmp(argv[1], "clock") == 0) {
        printf("clock|%s|%" PRId64 "|%" PRId64 "\n", boot, nanos(CLOCK_MONOTONIC), nanos(CLOCK_BOOTTIME));
        return 0;
    }
    if (argc != 5 || strcmp(argv[1], "launch") || !identifier(argv[2]) || !identifier(argv[4])
        || (strcmp(argv[3], "reference") && strcmp(argv[3], "tenfold"))) return 13;
    /* A nonzero pidof result alone is insufficient: require its exact normal absence status
     * and empty output. This fixed command cannot target another application. */
    FILE *process = popen("/system/bin/pidof pro.vafadar.zanance.androidhomereview", "r");
    if (!process) return 14;
    char output[64] = {0};
    const int found = fgets(output, sizeof(output), process) != NULL;
    const int status = pclose(process);
    if (found || !WIFEXITED(status) || WEXITSTATUS(status) != 1) return 15;
    char marker[128];
    snprintf(marker, sizeof(marker), "/data/local/tmp/.zanance-d574-%s.request", argv[4]);
    const int descriptor = open(marker, O_WRONLY | O_CREAT | O_EXCL | O_NOFOLLOW, 0600);
    if (descriptor < 0) return 16;
    if (write(descriptor, argv[4], 32) != 32 || fsync(descriptor) != 0 || close(descriptor) != 0) return 17;
    const int64_t monotonic = nanos(CLOCK_MONOTONIC), boottime = nanos(CLOCK_BOOTTIME);
    if (monotonic <= 0 || boottime < monotonic) return 18;
    char start[32], elapsed[32];
    snprintf(start, sizeof(start), "%" PRId64, monotonic);
    snprintf(elapsed, sizeof(elapsed), "%" PRId64, boottime);
    printf("launch|%s|%s|%s|%s|%s|%s|absent\n", argv[2], argv[3], argv[4], boot, start, elapsed);
    if (fflush(stdout) != 0) return 19;
    /* The interval includes this output flush and command-dispatch overhead. It is
     * intentionally distinct from the OS launch timestamp and cannot understate it. */
    char *const command[] = {"am", "start", "-W", "-f", "0x10008000", "-n",
        "pro.vafadar.zanance.androidhomereview/crc649b2744da976581ee.MainActivity",
        "--es", "review_phase", "cold-shell-measure", "--es", "review_run", argv[2],
        "--es", "review_shape", argv[3], "--es", "review_request", argv[4],
        "--es", "review_launch_boot", boot, "--el", "review_launch_monotonic", start,
        "--el", "review_launch_boottime", elapsed, "--ez", "review_process_absent", "true", NULL};
    execv("/system/bin/am", command);
    return 20;
}
