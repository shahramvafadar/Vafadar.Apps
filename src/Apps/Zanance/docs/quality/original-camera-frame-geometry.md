# Original camera-frame geometry (D-609)

## Uncovered assertion and unchanged inputs

D-598 proves that its original camera recognition and compact stored frame lose
the same header/footer letters, while the unchanged packaged PNG is read completely.
The remaining question here is narrower than another OCR or financial scenario:
does the existing frame preserve the original receipt's two-dimensional geometry?
Read only the two original local files, verify their original digests before decoding,
and never resize, crop, replace, annotate or save altered images.

| Original witness | Dimensions | SHA256 |
|---|---|---|
| Fictitious input PNG | 1280x960 | `4901841bb569d381d69df760e5d448446102697d377c42b860bea4080eee6bac` |
| Metadata-free stored JPEG | 960x1280 | `7462be94e722942b37133293b2b2076139bd945ae1d21ce4ca4cb91d9917b93b` |

The exact input date remains27October2026. Do not replace it with today's date or
repeat D-597 cancellation, D-598 shutter/Done, reader calls, Save or attachment work.
No app/AVD/phone command, new recording, image synthesis or financial operation occurs.

## Measured original ink geometry

Decode each existing file without writing an image. For each original row, collect
the inclusive bounding box of pixels whose three RGB channels are below80, then
group adjacent nonempty rows. Both files have exactly six ordered line groups.
Coordinates below are inclusive `[left, top, right, bottom]` in original file pixels.

| Line | Input box | Stored box |
|---|---|---|
| Header | 121,131,829,175 | 161,175,959,234 |
| Date | 122,239,358,273 | 163,319,478,364 |
| Book | 124,369,683,403 | 165,492,911,538 |
| Coffee | 123,459,675,493 | 164,612,900,658 |
| Total | 121,621,626,665 | 161,828,835,887 |
| Footer | 124,789,808,823 | 165,1052,955,1098 |

The stored height/input height ratio is exactly4/3. Every line's top and left ink
positions agree with multiplication by4/3 within1/3pixel. The four complete middle
lines' right positions agree within2/3pixel. This is consistent with a uniform
height-derived enlargement anchored near the original top-left, not a rotation of
the complete landscape receipt into a complete portrait frame.

Under that measured mapping, the960-pixel stored width covers only720 input pixels,
56.25% of the original1280-pixel width. This visible-input range is an inference from
the measured mapping, not a separately observed raw sensor crop rectangle. The header
reaches the actual stored right edge959; its projected original right ink position
would be1105.33. The footer's projected right ink would be1077.33, beyond that edge;
its last surviving ink stops at955. Both missing word endings therefore lie outside
the observed frame. No missing letters or pixels are reconstructed.

## Product boundary and source-version limit

The current original Android `CameraCapture.TakePhotoAsync` supplies the external
output URI and reads the returned file bytes. `DocumentImages.PrepareAndroid` decodes
the complete bitmap rectangle, applies bounded scale/orientation and re-encodes it;
it does not select a smaller source rectangle. D-598 already records960x1280 for both
recognition input and stored copy, with identical recognized text. This evidence
does not justify changing receipt interpretation, merchant guesses or financial totals.

The official [emulator command-line documentation](https://developer.android.com/studio/run/emulator-commandline)
defines `imagefile:` as the camera's file input; it does not promise byte-preserving
framing. The installed host package metadata reports37.1.11/build15917651. The inspected
public `emu-master-dev` source properties report35.6.3 and `emu-main-dev` reports37.3.6;
neither establishes the exact installed implementation. Preserve those mismatches;
do not attribute this result to a particular upstream renderer line or claim the
exclusive internal emulator/Camera2 cause. No emulator update or dependency is added.

## Completion and remaining input

The original-frame geometric question is answered using retained evidence only.
This adds no managed/native test count, new build/package, product change or whole-text
camera acceptance. No cleanup or successful prior scenario is repeated. Private
measurements and primary-source fetch receipts remain at `%TEMP%\Zanance-D609-current.txt`;
the original D-598 image and native receipts remain unchanged.

QA-05 still needs a genuinely completely framed supported receipt from the original
camera route, with complete merchant/text evidence. It cannot be satisfied by padding
the existing input to force the old cohort to pass, inventing clipped text, re-running
an accepted Save, or relabelling this stored frame. Physical camera scene availability
and a broader admissible receipt corpus remain actual inputs. General stability,
security, accessibility, iOS, provider/Store/publication and the complete delivery
goal remain open; D-594's TalkBack execution order is unchanged.

## Consumed one-use hosted follow-up

[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40618142579): completed/success. [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40618142568): completed/cancelled.
Only the original single exact-head response is used. Its consumed marker and response
are retained; only that one-use monitor is deleted. No second request or broader release
conclusion is inferred. Cancellation cause and an unfinished CI result are not guessed.
