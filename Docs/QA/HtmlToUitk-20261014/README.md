# HtmlToUitk: portable native proof

Дата: 2026-10-14. Проверено ближайшее заявленное подмножество по IB:705–706. Сводка scope/source/ограничений — [Verification](../../Plans/HtmlToUitk-Verification-20261014.md); расширения — [TODO](../../Plans/HtmlToUitk-TODO.md). Это actual operator results, не synthetic screenshot или typecheck.

**Actual native input manifest:** `e5ed8df321f38a6e6572f808d59db313f4dc32968167b88e153af6910bec6f25`. CLI input manifest: `24337a5ed949d4a9ced9cae7064e810627d2b4fff4d262907eaf62fc23ff21f1`. **Публикуемый runtime manifest:** `a9c0cd0ee76ffd70a9a926444026e38785192cde0a36f3c0667f53b13edb1043`;163/163 worktree/index hashes совпали. Он сохраняет E5ED input identity и163 raw→LF mappings:60CRLF→LF, non-line-ending/GUID drift0. Нового native прогона A9C не было; lossless mapping не подменяет actual native input SHA. Проверенное заявленное подмножество принято; расширенный TODO открыт. Публикация Git пока Pending.

XML sanitization заменяет только machine-local QA/source path prefixes на `[QA_ROOT]/` и `[SOURCE_ROOT]/`. Test names, counts, assertion results, timestamps и значения сохранены; parsed XML сравнен после той же path mapping. Completion JSON скопированы без изменения результатов. Capture PNG и pixel JSON скопированы побайтно и сверены SHA; никаких credentials/user-profile paths в public proof не найдено.

| Actual native run | Результат / exit | Original operator XML SHA256 | Public sanitized XML SHA256 |
|---|---|---|---|
| [6000.6 EditMode](editmode-6000.6-final.xml), [completion](editmode-6000.6-final-completion.json) |239/239 passed,0fail/0skip; exit0 | `b9e3929618520520208f633b3792baa29e6da856641f21391f800235690ef32b` | `afe597ce43c3df04faace4e79d41c44a2ced01b6d11c96753cb9354baac4783b` |
| [6000.6 PlayMode](playmode-6000.6-final.xml), [completion](playmode-6000.6-final-completion.json) |7/7 passed,0fail/0skip; exit0 | `bd25ee59a28356ad4087d67672d53a8153cf87221c1ddde84f87cfecc19d9dc7` | `edc7d0c744c07971f96a2e33d95b4834e1930c0d375e67cc9e301f517b330904` |
| [6000.3.14 EditMode](editmode-6000.3-final.xml), [completion](editmode-6000.3-final-completion.json) |239/239 passed,0fail/0skip; exit0 | `e7a83d3e981eff58760063356eaa4b8546aa5c7528ba3bbee35ae9674bd41a2b` | `6337cf8925a0ae96c94a00bf48d92b64a5dd1d1a832bf0e17a3ad6a0003a7abf` |

PlayMode7/7 проверяет already-loaded UIDocument/PanelRenderer reload, реальные pointer/keyboard actions once после detach/reload, nav/modal/tabs/focus/cancel, media resize, authored gradient geometry/pixels, scheduled animation/reduced motion/dispose. Final correction касался внешнего QA driver: отдельная видимая action button вместо перекрытой unconfigured modal и официальный default runtime theme. Assertions не ослаблялись; generated UXML/USS не менялись.

Реальный final7-test harness сохранён как [NativePlayTests.cs.txt](NativePlayTests.cs.txt), вне Unity Assets и runtime package. Для повторения: создать чистый Unity QA project с текущим runtime/editor; конвертировать shipped production-neutral fixture в `Assets/QA/Generated`, подключить NUnit/Unity Test Framework, перенести этот actual harness в test-only PlayMode assembly, назначить официальный default runtime theme согласно его `Assets/QA/DefaultRuntimeTheme.tss`/`Resources/NativeQa` lookup, затем Test Runner. XML/captures сохранять вне package. Это воспроизводимый source/context, не автоматически установленный polished demo и не player build claim.

## Final render proof

Graphics API: Direct3D11; color space: Gamma; Unity6000.6.0f1. Не переносить этот результат на mobile/Linear/другие renderer без отдельного прогона.

| Artifact | SHA256 |
|---|---|
| [Gradient1280×720](captures/gradients-1280x720.png) | `877bb7f6c1e650dd7d0ca91194ce7239de64e691616963ffb1e34bbb2f6b49b1` |
| [Gradient1920×1080](captures/gradients-1920x1080.png) | `da3d5199faf25285a7d30681b2122000d7886f543f61b7c97e4500b644c1872c` |
| [Gradient3440×1440](captures/gradients-3440x1440.png) | `dc41fd8cd2fc2eb78aa34a56cc53a08f60a968a0cc2230f35fb8700893afe611` |
| [Pixels1280×720](captures/gradient-pixels-1280x720.json) | `f59e30ae69b4894adb4dd67bb4c366e75676b654f82941aaab4b538173a96854` |
| [Pixels1920×1080](captures/gradient-pixels-1920x1080.json) | `25eed4e32b2a8e6e44a563d6f6097e686e21ed8e0e0f405c35e5d4a11523e665` |
| [Pixels3440×1440](captures/gradient-pixels-3440x1440.json) | `103b1541fb31c410bc6ba67e7f75db3acbcffd97a3c29ff23c12b9afa80ac1da` |
| [Home1920×1080](captures/home-1920x1080.png) | `40cf4c98f136f6cfae2a4c4ce73a396956fd047403d0a21e9ab7a13f330b0acf` |
| [Modal closed1280×720](captures/home-modal-closed-1280x720.png) | `9d7f83ac4963501a3dd8e2f4a39ac001632280dd4b85a2d1d4dda7a83e25bd92` |
| [Settings/video1280×720](captures/settings-tab-video-1280x720.png) | `d998c063f7caf9021d1cf34fe8a03575da9bf21298f593a5fb3d98eacf559465` |

Root просмотрел final home1920/modal-closed1280/settings-video1280: native buttons и соответствующие nav состояния рендерятся; gallery градиентов также есть в том же fixture. Прежние misleading home filenames и black captures относятся к старому QA host и не используются как final visual proof.

**Граница приёмки:** behavioral/native controls/render presence и gradient sampled pixels PASS. Fixture не назначает ILocalizer, поэтому видны ключи `app.home.*`; API ILocalizer/Relocalize документирован, но localization sample binding здесь не принят. Black-on-black contrast, partial animation view и общий вид не объявляются polished UX или browser fidelity. Удобный styled landscape sample с localizer/table/theme/contrast, practical Button example и independent HTML/Unity visual comparison остаётся TODO; стиль проекта сейчас не исправляется.

История не скрыта: ранний Edit239/238/1 и Play4/7, затем renderhost1 7/6/1 были fail. Historical XML digests: `731b8e46783e7d4de4ac92fcdf47459f49316600ecc73ecc09015a490aacdbde` (6000.3 initial), `d0c88626359abc9f2084c85dda0801b8139b12e4c8018eec12ff39402f36cd06` (6000.6 Edit2), `d230e05881e20f27bd6a80645612c8f03e444e81a5dfaf083cf332aceff7b431` (renderhost1). Black PNG не входят в final proof. Final passed counts относятся только к сохранённым native inputs/receipts выше.
