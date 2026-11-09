# LiveLogPipeline設計書

## UseCase検討

Primary(自分の必要性)

- Generic Host 上で動作するデスクトップ・クライアントアプリ（WPF等）において、Microsoft.Extensions.Logging（ILogger）経由で発行されたログを一時的なリングバッファに安全に蓄積し、UI のログ表示画面や診断ビューワーへ高スループット・低アロケーションでバッチ配信するデータパイプライン

----

Second

- 障害・クラッシュ時の「直近ログのスナップショット取得（Pull型）」
- ヘッドレスサービス（Web/Blazor/CLI）におけるリアルタイムストリーミング
- CI/CD や結合テストでのログキャプチャ
