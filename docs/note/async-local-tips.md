# AsyncLocalについて

`AsyncLocal`クラスは以下のようにDocumentに記載があります。

> Represents ambient data that is local to a given asynchronous control flow, such as an asynchronous method.
> Remarks  
> Because the task-based asynchronous programming model tends to abstract the use of threads, AsyncLocal\<T\> instances can be used to persist data across threads.  
> The AsyncLocal\<T\> class also provides optional notifications when the value associated with the current thread changes, either because it was explicitly changed by setting the Value property, or implicitly changed when the thread encountered an await or other context transition.

ざっくり言えば、Threadを超えたデータを維持するための仕組らしい。ASP.NETCoreやLogger.Scopeなどでもこれらの実装を使ってDIで解決されるScopedインスタンス内での実行中に、Scoped特有の情報を共有するのにつかわれるみたい。
AsyncLocalのデータの共有の境界は誰かが、AsyncLocalに値を代入した時点から、その非同期フローが分岐・終了するまでという形式になる。なので、使いたい場合は、ちゃんと自分でSetをして制御してあげないといけない。ちなみにこの仕組みの裏側ではExecutionContextという仕組みが動いているみたい。

ExecutionContextについては、Runtimeが割といい感じに実行情報みたいなものを裏側で受話つしてくれているものらしい。なので、AsyncLocalについてはそれがうまく管理してくれているみたい。

なので、大昔に作ったScopeManagerについては、AsyncLocalを使うことでScopeIDクラスなんぞを作らなくてもScopeIDの情報を手に入れることができるらしい。マジかよ。→こちらはライブラリとして固められる範囲だから割とこう相性な気もする。ただユーザがScopeIDをとれる環境を準備してあげないといけないから微妙かなあ。

また、スコープ設定値みたいな感じのクラスを作ったこともあるけど、実はこれもAsyncLocalを使うことで余分なクラスにすることができそう。→ただ、通常のケースなら普通にインジェクションによる解決法が自然な気がする。AsyncLocalの値を書き換えられない制約がつけられないからわかってない人が変えちゃうとScopeが壊れる可能性もある。

どちらかというとライブラリ向けの機能な気がするけどTIPSとして覚えておくと面白いことができるかもしれない。より狭い機能にThreadLocalというクラスもある。こちらは厳密にスレッドが同じ

## 参考

[Ref1](https://learn.microsoft.com/ja-jp/dotnet/api/system.threading.asynclocal-1?view=net-10.0)
[Ref2](https://devblogs.microsoft.com/dotnet/executioncontext-vs-synchronizationcontext/)
[Ref3](https://learn.microsoft.com/ja-jp/aspnet/core/fundamentals/use-http-context?view=aspnetcore-10.0)
[Ref4](https://learn.microsoft.com/ja-jp/dotnet/core/extensions/logging/overview?tabs=command-line#log-scopes)
[Ref5](https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Logging.Abstractions/src/LoggerExternalScopeProvider.cs)