# Angel sequence v1 — static review / Flow references

Generated using the built-in image generation tool, 2026-10-21. These are pose candidates, NOT a verified animation or Unity-ready transparent effect.

Files: 01-card-only.png → 02-wings-unfurled.png → 03-wings-embrace.png.

Measured sizes: frames 01 and 02 are 1672×941; frame 03 is 1671×941 (one-pixel generated-width difference). Normalize the final frame before production compositing; the current first 01→02 test uses equal-sized inputs.

The two visible-card frames use the same apparent card location and size. Exact pixel identity and motion continuity are not guaranteed by AI edits. The final card is occluded, not transformed. All backgrounds are deliberately black, not transparent. Video compositing/transparency must be prepared separately before Unity integration. No Unity scenes, scripts, timings or gameplay rules were changed.

First test: generate ONLY 01→02 (unfolding from behind card). After review, separately generate 02→03 (cupping/embracing, no reopening). Do not pay for both tests in advance. Review the actual generation cost before running in Flow. Card-centered bloom and gameplay swap belong to a later Unity integration step, not to these clips.

## First Flow test prompt

Locked frontal camera. The central card stays perfectly still and unchanged. Exactly two full-sized ivory and gold illustrated angel wings are initially folded and completely hidden BEHIND the card. Both wings smoothly unfold outward from behind its side edges, revealing their existing full feather length and matching the ending frame. They do NOT grow from tiny wings or simply scale up. Keep their roots attached behind the card. Preserve the hand-painted style. Hold the open pose briefly at the end; do not close or reopen. Uniform pure black background throughout. No glow, haze, particles, background changes, camera movement or card deformation.

## Image prompt: Open master

Use case: compositing. Asset type: animation KEYFRAME B, WINGS FULLY UNFURLED BEHIND THE CARD, one still.
References: image 1 is the Heaven card art; preserve its gold/blue rectangular frame and original angel illustration. Image 2 is reference ONLY for the exact illustrated ivory feathers and gold contour design of the TWO external wings. Image 3 is reference only for their later embracing pose; do not draw that pose here.
Canvas 1536x864 (16:9). Frontal locked camera. PURE SOLID BLACK #000000 background everywhere outside objects, NO ambient glow whatsoever.
Geometry: exactly ONE upright Heaven card at center (768,432), card bounds x636..900 y240..624, width264 height384, preserving source artwork aspect ratio. Card stays entirely visible, in FRONT of both wing roots. Exactly TWO LARGE wings unfold from BEHIND the card's left and right mid-height edges (636,432) and (900,432). Wings are physical full-sized adult angel wings, NOT tiny wing buds. Left wing arches wide to the left, right wing to the right. Each wing's curved shoulder/elbow and layered full-length primaries show graceful natural anatomy. Each wing individually has a visible fan length comparable to or greater than the CARD HEIGHT. Total wing span about 1200 pixels, left tips no farther left than x160 and right tips no farther right than1376. Wing tips stay between y90..775 with black margin. Balanced bilateral symmetry, gentle open pose poised to cup forward next. Do not cross or cover the card yet.
Style: hand-illustrated fantasy game art matching reference 2, ivory-white feather planes with fine golden edges, restrained painted shading, no realistic feather microtexture. Exactly one pair of external wings. The wings printed INSIDE the card remain part of the original art and never extend individually out of it.
CRITICAL: no global light, no gold haze, no gradient background, no bloom, no vignette, no particles, no fog, no floor, no shadows cast on background. Only card and two wings on FLAT UNIFORM PURE BLACK. Gold is confined INSIDE feather outlines and card artwork. No labels or text outside card. One keyframe, not a storyboard sheet.

## Image prompt: Card-only edit

Use case: precise-object-edit. This supplied image is the exact master composition for an animation. Create KEYFRAME A: CARD ONLY, before the wings unfold from behind it.
Remove ONLY the two external wings sticking out to the left and right of the rectangular card, replace their pixels with uniform PURE BLACK #000000 matching the existing background. Keep the single central Heaven card EXACTLY unchanged, including the angel and all wings printed WITHIN the card artwork, its original gold/blue frame, pixel position, dimensions and sharpness. Do not remove feathers printed INSIDE the card, do not redesign or regenerate the art.
Keep full original 1672x941 canvas and straight-on camera, no crop, no resize, no centering adjustment. Card occupies x611..1061, y157..805 in this supplied image; keep that exact apparent rectangle. The external physical wings are completely hidden behind this card at this initial moment. Draw no feather tips outside its border. Result is simply the exact same card on pure black. No glow, haze, gradient, particles, shadows, text, or additional objects. Single still keyframe.

## Image prompt: Embrace edit

Use case: precise-object-edit. This supplied image is the exact master composition for an animation. Create KEYFRAME C: THE SAME TWO WINGS SOFTLY CUP THE CARD FROM BOTH SIDES. Change ONLY external wing pose. Preserve canvas 1672x941, camera, wing identity, feather lengths, ivory-white illustrated planes and fine gold outlines. NO camera zoom, no object scale change.
The upright rectangular card remains fixed x611..1061 y157..805, and has NOT moved, shrunk, flipped, or transformed. It is now OCCLUDED by feathers coming forward in front of it. External wing roots stay anchored behind the midpoint of each card edge, about (611,420) and (1061,420), NOT relocated to upper center.
Both full-size wings curve through shoulder/elbow FORWARD around the sides of the card toward camera and then inward, like two arms giving a soft protective hug. Equal left/right visual weight, two distinct rounded shoulders. Long primaries layer diagonally downward/inward and meet near the vertical center in a soft shallow overlap. Feather fans remain broad and rounded with visible volume; do not pack tightly, crumple, braid, twist, crush, squash, turn upside down, or make a thin cylinder. Neither wing sweeps across the entire foreground. End in a relaxed enclosing oval, not an extreme tightly folded pose. Wing tips gently overlap enough to cover the entire card, including its frame; no card should show THROUGH feathers. The closed pair's overall height remains comparable to this master's open wings; it does NOT become a tiny object. Approximate envelope x430..1242 y100..860, with generous surrounding black margin.
Background remains flat uniform PURE BLACK #000000. No gold haze, no bloom, no background lighting, no added glow, particles, smoke, motion trails, text, frame borders or scenery. Exactly TWO external wings only. Hand-illustrated fantasy artwork, not photorealism. Single still, not animation or sprite sheet.

## Image prompt: Embrace balancing edit

Use case: precise-object-edit.
Edit image 1, the current closed pair on pure black. Image 2 is the APPROVED POSE BALANCE reference, NOT its background. Correct ONE thing: remove the dominant left foreground wing crossing over almost the entire right wing. Make both wings equally visible and equally participating in the hug, matching image 2's balanced central seam.
Preserve image 1's 1672x941 canvas, position, overall pair height, ivory feathers and fine gold outlines, and flat PURE BLACK background. Exactly two broad softly curved wings, protective two-armed cupping gesture, near bilateral symmetry, left shoulder and right shoulder at equal height, mirrored curvature. Each side occupies its own half and curves forward/inward around the hidden upright card; only narrow feather tips softly overlap near the vertical centerline. No long left-to-right diagonal sash, no crossed arms, no flattened feather panels, no braid or narrow tight cylinder. Rounded relaxed envelope with volume, not an extreme compressed fold. Card remains concealed. No glow, haze, gradient, symbols, extra objects or text. No camera/scale changes. Keep background #000000 uniform, not the gold haze of image 2.

