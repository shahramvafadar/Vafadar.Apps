/* Only the disposable signal-delivery qualification links this controlled fault origin. */
__attribute__((visibility("default"))) void zanance_fault_origin(void *page);
void zanance_fault_origin(void *page)
{
    /* The caller owns an intentionally inaccessible anonymous page, never a financial buffer. */
    *(volatile unsigned char *)page = 1;
}

/* A fixed instruction permits the controlled previous handler to recover concurrently without unwinding. */
__attribute__((naked, visibility("default"))) void zanance_fault_origin_recoverable(void *page);
void zanance_fault_origin_recoverable(void *page)
{
#if defined(__aarch64__)
    __asm__ volatile(".global zanance_fault_origin_instruction\nzanance_fault_origin_instruction:\nstrb wzr, [x0]\nret");
#elif defined(__x86_64__)
    __asm__ volatile(".global zanance_fault_origin_instruction\nzanance_fault_origin_instruction:\nmovb $1, (%rdi)\nret");
#else
#error Controlled instruction recovery is qualified only for ARM64 and x64.
#endif
}
