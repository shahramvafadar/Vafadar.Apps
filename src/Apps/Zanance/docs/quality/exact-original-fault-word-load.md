# Exact original faulting word load (D-712)

## Missing operation, not another workload

D-679 admits the original D-678 PID10110/UID10216 fault PC0x2edd7d inside
major_scan_ptr_field_with_evacuation, but its optimized inner source line is
unavailable. D-700 decodes the separate client/card-table functions and excludes an
already present non-array correction. D-708/D-711 bounded managed inputs do not
reproduce the original crash. None of their native, financial or UI work is replayed.

The new question is the exact machine operation at the original top fault PC.
It is answered through static original-file bytes alone. No app/emulator/phone,
private heap, real register value, fault address, raw tombstone or database is read.
No product/runtime/GC/configuration change, compilation or test is needed.

## Revalidate the original input

The original public package ELF remains3290280bytes, SHA256
edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4.
The preserved used official debug file remains15762568bytes, SHA256
8b4efcee70b5e35f14a7070a26b145d074f7b2fa62e4418f590fed658e9a88d0.
Re-read only those admitted artifacts; no refused decoder/symbol-cache scope is used.
Both GNU BuildIDs equal b9db63cc66e53a97dc8483280f7aab8a31672169 and the original
GNU debug-link filename/CRC match. The actual positive-size STT_FUNC symbol uniquely
admits start0x2edd40/length1001, containing the original PC at offset61.

Map the whole range into one actual executable PT_LOAD file range. Its1001bytes
have SHA256725296ba878e02c317e26fd4b035ca5d5452ca1f452a08cb3bec58aa6afc85fa.
This is no nearest-function guess, raw-memory export or extrapolation from a symbol.

## Static byte-preserving method

An already installed x64 static-code utility has a valid existing Authenticode
signature, version14.51.36257.0 and SHA256
ef58d7f394120709d84444296e534a17ee3f0decb493284568d81c0b96805169.
No decoder package, tool installation, PATH/system change or frozen tool reuse occurs.
Its [documented static code-section mode](https://learn.microsoft.com/en-us/cpp/build/reference/disasm?view=msvc-170)
reads a native code section. To supply original ELF bytes without executing them,
create one temporary [AMD64 COFF object section](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format):
20byte file header,40byte section header, optional-header size0, section RVA0 and
exact original1001byte payload. The1061byte object is not a linked/runnable image.
Its payload is byte-equal to the original function; object-relative offsets are
mapped back by adding0x2edd40, never treated as live process addresses.

Parse every instruction and wrapped byte-continuation line. All283 instructions
cover the complete1001 original bytes consecutively, each with matching bytes,
valid1..15byte length and no invalid decode. The top PC is an actual instruction
boundary at offset61. The object does not relocate code or make an external-call
address printed in object coordinates authoritative. There is no execution of the
original native payload.

## Actual instruction and bounded data flow

| Original module-relative PC | Original bytes | Static instruction / observation |
|---|---|---|
|0x2edd4b|48 8b 1e|mov rbx,qword ptr [rsi]: load the scanned field's pointer value on the normal entry path|
|0x2edd4e|48 85 db|test rbx,rbx|
|0x2edd51|0f 84 6b 02 00 00|zero-branch in that entry prefix|
|0x2edd7a|4d 8b 17|a separate word load through r15, before the original fault PC|
|0x2edd7d|48 8b 3b|mov rdi,qword ptr [rbx]: the original faulting eight-byte read|

The last operation is a data load, not an indirect call or the earlier field-slot
load. Independently inspect its admitted bytes: REX.W is set, opcode8B selects the
register-from-memory move, and ModR/M3B encodes no displacement/SIB, destinationRDI
and memory baseRBX. This agrees with the existing decoder. No direct jump target
into offset61 is found in the complete decoded function. For ordinary entry through
the function start, the displayed prefix loads the field value and tests zero before
that word read. This is not a recovery of the actual runtime register values or a
claim that corrupted control flow is impossible.

The original caller was already located in reference-array card scanning. The same
original [drain-gray-stack source](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-marksweep-drain-gray-stack.h#L54)
contains object-header/vtable-word loads in both source branches. Its raw SHA256
50ab37ab96e90bffa48373ab18865a887b228219b29ab370296e4787cc3c26f6 matches the
D-679 source receipt; raw lines54/137 contain those loads and line321 remains the
HANDLE_PTR invocation. Optimized inner DWARF line0 remains unavailable: the machine
operation is now proven, but a unique original C statement/branch is not invented.

This narrows the original fault to the expected referenced-object first-word read.
It does not reveal the actual pointer value, fault address, header contents, managed
array/element type, earlier writer, freed-object origin or an exclusive runtime/app
cause. It supplies no justification to change null handling, suppress the read,
catch/retry a native signal, change financial source or switch collectors. Further
repair needs original reference-origin/native-context evidence or a verified
correction for this actual operation.

## Retirement and scope

The temporary1061byte object is hash/size checked and removed through one exact
nonrecursive file deletion. Original module/debug artifacts, complete textual decode,
input identities, header receipts and byte-field checks are retained. There is no
new tool/cache/build output tree and no new deletion refusal. Existing refused
scopes remain frozen without retry/subset/alternate/move/escalation/reuse.

Product sources and prior D-710 strict Windows/fullshipping09140d86 remain unchanged;
no old successful test/build, new product test count or native workload is claimed.
D-711's own final one-shot CI follow-up remains separate. Phone/D-607's three
deadlines, consumed reboot, unread post-reboot source and Main-adoption condition
are untouched. Wider stability, performance, DB/key/security, accessibility,
iOS/provider/Store/release and the overall delivery remain open.
