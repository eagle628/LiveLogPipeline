# LiveLogPipeline設計書

## UseCase検討

Primary(自分の必要性)

- Generic Host 上で動作するデスクトップ・クライアントアプリ（WPF等）において、Microsoft.Extensions.Logging（ILogger）経由で発行されたログを一時的なリングバッファに安全に蓄積し、UI のログ表示画面や診断ビューワーへ高スループット・低アロケーションでバッチ配信するデータパイプライン

----

Second

- 障害・クラッシュ時の「直近ログのスナップショット取得（Pull型）」
- ヘッドレスサービス（Web/Blazor/CLI）におけるリアルタイムストリーミング
- CI/CD や結合テストでのログキャプチャ

## Requirements検討

### 蓄積・データ構造 (Ingestion & Storage)

- `Microsoft.Extensions.Logging` の `ILogger` からのログデータを直近 N 件までインメモリに保管する。
  - バッファ上限（MaxCapacity）超過時は、最も古いエントリから自動破棄（DropOldest 循環）する。
- 書き込みの Lock は厳禁。ロックフリー指向の設計とする。
  - `ILogger.Log` 呼び出しはロックフリー（`System.Threading.Channels` の非ブロッキング書き込み）を前提とし、呼び出し元スレッドを一切ブロックしない。
  - 初期フェーズでは実装コストと安全性を優先し、内部バッファ（Channel）経由の単一 Consumer 集約パイプライン構成とする。
  - 将来的な最適化（$2^n$ シーケンス配列直書き）への差し替えを見据え、外部インターフェース（`ILiveLogPipeline`）は内部構造に依存しない設計とする。
- 保持する`LogEntry`レコードは値型想定

### 読み出し・配信 (Consumption & Delivery)

- 保管されたログデータを取り出すことができる（Pull および Push の両対応）。
  - **Push（リアルタイム配信）**:
    - 定周期バッチ集約（Periodic Batching / Throttle: 最大遅延時間 N ms または 最大保持件数 M 件に達したらフラッシュ）を基本とし、オプションとして `Debounce`（ログ静止時のみフラッシュ）への切り替えを可能とする
    - 時間経過やタイマー駆動の処理は、決定論的かつ高速な単体テストを実現するため、すべて `TimeProvider`（.NET 8+）経由で実行すること。
  - **Push における取り出しメモリ方針**:
    - Mode A (Delta): 直前の通知からの更新分のみを取得
      - Fast パス (Zero-Allocation):
        - コールバックに `ReadOnlySpan<LogEntry>` を使用し、非同期持ち出しを言語仕様（ref struct）で物理的に遮断。
        - 通知用バッファは `ArrayPool<LogEntry>` を活用し、イベント終了直後にライブラリが自動回収する。
      - Safe パス (Async/Dispatcher 互換):
        - 非同期処理や Dispatcher.InvokeAsync への安全な引き渡しを目的とし、バッチごとの独立配列（`LogEntry[]` / `ReadOnlyMemory<LogEntry>`）生成を許容する。
    - Mode B (Snapshot): 通知時点のバッファ内全件を取得（低頻度な同期利用または明示的なスナップショット配列生成とする）。
  - **Pull（オンデマンド取得）におけるメモリ方針**:
    - 手軽な取得用: 呼び出し時に新しいスナップショット配列を生成して返却（`LogEntry[] GetSnapshot()`）。
    - ゼロアロケーション用: 呼び出し側が渡したバッファへ直接書き込み（`int CopyTo(Span<LogEntry> destination)`）。

### フィルタリングと開発者体験 (Filtering & DX)

- ログのフィルタリングが可能となり、インメモリに保存する対象範囲を指定できること（`CategoryName`, `LogLevel`, `EventId` 等）。
- 開発用ログとユーザー向けログを別々の `ILogger` インスタンスに分けて実装しなくてよいこと。
  - 単一の `ILogger<T>` から発行されたログに対し、`EventId` 等のタグを用いてインメモリ保存対象を透過的に選別できること。
- `Microsoft.Extensions.Logging` の Provider 独立フィルタ機能および本ライブラリ固有の Options（`EventIdFilter` 等）により、ファイル出力（FileLogger）など他のログプロバイダの設定や動作と相互に干渉・依存しないこと。

### 構成・設定 (Configuration)

- 保管上限件数、バッチフラッシュ間隔、フィルタ条件などを `appsettings.json`（Options パターン）やコードから設定可能とする。

### 責務境界とメモリライフサイクル

- **LiveLogPipeline の責務**:
  - インメモリバッファの容量（MaxCapacity = N）を厳密に保証し、超過時は DropOldest で自動破棄する。
  - 外部への Push 通知は設定された配信モード（Delta または Snapshot）に従って安全にディスパッチし、自身のメモリ使用量を一定（O(1)）に保つ。
- **UI / Consumer の責務**:
  - UI 側の表示リスト（`ObservableCollection` 等）の肥大化を防ぐため、UI 側での最大表示件数（TrimExcess）の制御は ViewModel / UI 側の責務とする。
  - （例: UI 側でバッチ差分を追加後、`while (Logs.Count > MaxViewCount) Logs.RemoveAt(0);` を行う運用を標準とする）
