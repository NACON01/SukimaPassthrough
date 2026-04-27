# SukimaPassthrough — プロジェクトコンテキスト

## 概要
Unity 6 (6000.0.53f1) + Meta XR SDK Core 201 + Quest Pro (Air Link) の HCI 研究プロジェクト。
**視線が下方 15° 以上に 0.2 秒滞留したら、鼻と HMD の隙間に対応する視野下部にパススルー窓を開く。**

## 環境
- SDK: Meta XR SDK Core 201 (OpenXR バックエンド、Oculus XR Plugin は未使用)
- OpenXR Plugin: 1.15.1、URP 17.0.4、Input System 1.14.0
- 接続: Air Link (USB ポート破損のため有線不可)
- Graphics API: D3D11、Render Mode: Single Pass Instanced

## アーキテクチャ

### 主要コンポーネント
| ファイル | 役割 |
|---|---|
| `Assets/Script/GazePassthroughController.cs` | 視線判定・パススルー窓フェード制御・UI スライダーバインド |
| `Assets/Script/ControllerLaserPointer.cs` | コントローラーレーザー表示（OVRCameraRig から右アンカー自動取得） |
| `Assets/Scenes/SampleScene.unity` | メインシーン（OVRCameraRig プレハブインスタンス） |
| `Assets/Oculus/OculusProjectConfig.asset` | Meta XR プロジェクト設定 |
| `Assets/XR/Settings/OpenXR Package Settings.asset` | OpenXR フィーチャー設定 |

### パススルー方式
**Selective Passthrough (Underlay + Reconstructed)**
- `OVRPassthroughLayer` (fileID 111222333): `projectionSurfaceType=0`, `overlayType=1`, `textureOpacity=1`
- Quad (fileID 885613680): `SelectivePassthrough.mat` でアルファ穴あけ、`_InvertedAlpha` で窓の開閉
- GazePassthroughController が `MaterialPropertyBlock` 経由で `_InvertedAlpha` を 0→1 にフェード

### 視線追跡
- OVREyeGaze (fileID 1629257527): `Eye=Left`, `ReferenceFrame=CenterEyeAnchor`, `TrackingMode=HeadSpace`
- ピッチ計算: `forward.y` ベース asin（ジンバルロック回避）
- ヒステリシス: 開く=15°、閉じる=10°

### コントローラー UI
- EventSystem に `OVRInputModule` 設定済み (`rayTransform=RightControllerAnchor`)
- Canvas (fileID 1887819323): WorldSpace、`OVRRaycaster` 付き
- クリックボタン: `OVRInput.Button.One` (A ボタン)
- `MetaQuestTouchProControllerProfile Standalone` 有効化済み

## 重要な設定値（変更禁止）
- `OVRManager.isInsightPassthroughEnabled = 1` (OVRCameraRig PrefabInstance オーバーライド)
- `m_BackGroundColor.a = 1` (Camera オーバーライド、0 に戻すと全視野パススルーになる)
- `MetaQuestTouchProControllerProfile Standalone: m_enabled = 1` (0 に戻すとコントローラー認識しない)
- VIVE OpenXR レイヤー: 環境変数 `DISABLE_XR_APILAYER_VIVE_*=1` で無効化済み (setx 済み)

## 既知の問題・未解決
- [ ] WorldSpace Canvas スライダーにコントローラーレーザーが当たらない (Canvas z=1.5 に修正済みだが未確認)
- [ ] `OculusProjectConfig.insightPassthroughEnabled` が Unity 再起動のたびに 0 に戻ることがある → 毎回確認が必要

## Quad パラメータ（GazePassthroughController Inspector 値）
```
verticalOffset = -0.35  (下方 35cm)
distance       = 0.5    (前方 50cm)
thresholdAngle = 15     (開く角度)
closeAngle     = 10     (閉じる角度、ヒステリシス)
dwellTime      = 0.2    (秒)
fadeSpeed      = 5      (1秒あたりの変化量)
```
Quad scale: (1.2, 0.15, 1) = 横 120cm × 縦 15cm のスリット

## 過去の主要なトラブルシュート
1. **HMD に映像が出ない**: VIVE Hub の OpenXR API レイヤーが xrCreateInstance を破壊していた → DISABLE_XR_APILAYER_VIVE_* 環境変数で解決
2. **全視野パススルー**: Camera.backgroundColor.a=0 でフレーム全体が透明 → a=1 に修正
3. **Quad が後ろ向き**: UserDefined + AddSurfaceGeometry の Z 軸反転バグ → Selective Passthrough 方式に移行
4. **コントローラー認識しない**: MetaQuestTouchProControllerProfile が無効 → 有効化で解決
5. **スライダー当たり判定なし**: Canvas が z=0.386 でコントローラーアンカー z≈0.4 より手前 → z=1.5 に移動

## セッション開始時のチェックリスト
1. `OculusProjectConfig.asset` の `insightPassthroughEnabled` が 1 か確認
2. `OpenXR Package Settings.asset` の `MetaQuestTouchProControllerProfile Standalone: m_enabled` が 1 か確認
3. Unity を**再起動**してから Air Link で Play（OpenXR プロファイル変更は再起動で反映）
