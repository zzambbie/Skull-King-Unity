# Surrender card integration

Seven framed, opaque 832x1216 PNGs are saved in `Assets/Art/Cards/CROSSION/Composed/`: surrender_01_card.png through surrender_04_card.png and alliance_01_card.png through alliance_03_card.png. Original artwork is preserved.

The four surrender cards use the white-flag border. The three special surrender cards use the silver/gold handshake border. Composition used the built-in image generator; the prompt is in surrender-cards-composition-prompt.txt. Preview: surrender-cards-preview.png. Download bundle: surrender-and-special-surrender-cards-832x1216.zip.

CardSpriteDatabase.asset now references these complete cards. Escape uses the existing four-artwork selection. Jewel uses the three alliance artworks selected stably by card ID, with jewelSprite as fallback. No extra frame is applied over the baked border. Existing full-card and Alt preview rendering use these same sprites. The editor card gallery includes the seven completed cards. ConfigureSurrenderCards provides a focused import/setup command under CROSSION/Cards/Apply Surrender Cards; the full Configure command also retains these mappings.

The existing 72-card deck does not create Jewel/special-surrender cards. This task registers their artwork and display lookup, but does not alter deck counts, card IDs, game rules or scoring.

Validation: all seven PNG dimensions and opacity checked; visually reviewed as a contact sheet. An isolated Unity 6000.3.10f1 project compiled CardData, CardSpriteDatabase and CressionCardAssetSetup, imported the seven sprites, and ran ConfigureSurrenderCards. The log contains SURRENDER_CARDS_OK and exited with code 0. Log: Logs/surrender-card-validation.log. The main project was already open, so no second editor was launched against it and no live Play Mode check was performed.
