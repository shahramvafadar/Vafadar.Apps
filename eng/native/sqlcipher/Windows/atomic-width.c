/* Exercises the unchanged upstream MSVC tsan_add macro, including 32-bit carry and concurrent updates. */
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <intrin.h>
#include <stdint.h>
#include <stdio.h>
#include <internal/tsan_assist.h>

typedef char require_int32[(sizeof(int) == 4) ? 1 : -1];
typedef char require_long64[(sizeof(LONG64) == 8) ? 1 : -1];
typedef struct {
    uint32_t before;
    TSAN_QUALIFIER int value;
    uint32_t after;
} packet32;

/* Keep separately inspectable functions so disassembly proves the selected intrinsic widths. */
__declspec(noinline) LONGLONG add32(TSAN_QUALIFIER int *value, int amount)
{
    return tsan_add(value, amount);
}

__declspec(noinline) LONGLONG add64(TSAN_QUALIFIER LONG64 *value, int amount)
{
    return tsan_add(value, amount);
}

static DWORD WINAPI worker(LPVOID input)
{
    packet32 *packet = (packet32 *)input;
    int i;
    for (i = 0; i < 200000; ++i)
        add32(&packet->value, 1);
    return 0;
}

int main(void)
{
    packet32 packet = { 0x7351ab29U, -1, 0x62ca81f2U };
    TSAN_QUALIFIER LONG64 wide = 0x100000000LL;
    HANDLE threads[4];
    int i;
    if (add32(&packet.value, 1) != -1 || packet.value != 0 || packet.after != 0x62ca81f2U)
        return 1; /* A mistaken 64-bit addition carries into the adjacent sentinel here. */
    if (add64(&wide, 7) != 0x100000000LL || wide != 0x100000007LL)
        return 2;
    for (i = 0; i < 4; ++i) {
        threads[i] = CreateThread(NULL, 0, worker, &packet, 0, NULL);
        if (threads[i] == NULL)
            return 3;
    }
    if (WaitForMultipleObjects(4, threads, TRUE, 30000) != WAIT_OBJECT_0)
        return 4;
    for (i = 0; i < 4; ++i)
        CloseHandle(threads[i]);
    if (packet.value != 800000 || packet.before != 0x7351ab29U || packet.after != 0x62ca81f2U)
        return 5;
    puts("Upstream atomic widths, carry sentinel and 800000 concurrent updates passed.");
    return 0;
}
