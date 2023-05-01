# 例外処理アーキテクチャ

つづいて例外処理アーキテクチャについて設計します。例外処理は、WPFとgRPCでまったく異なります。そのため、それぞれ個別に設計していきましょう。

## WPFの例外処理アーキテクチャ

WPFの例外処理は、特別な意図がある場合を除いて、標準で提供されている各種の例外ハンドラーで一括して処理することにします。

実際問題、起こりうる例外をすべて正しく把握して、個別に設計・実装することはそもそも現実味がありません。特定の例外のみ発生個所で個別に例外処理をしても、全体としての一貫性が失われることが多いです。また、例外の隠ぺいや必要なログ出力のもれにつながりやすいです。であれば、グローバルな例外ハンドラー系に基本的には任せて一貫した例外処理をまずは提供するべきかと思います。

ただもちろんすべてを否定するわけではありません。

たとえば、何らかのファイルを操作するときに、別のプロセスによって例外がでることは普通に考えられます。このような場合にシステムエラーとするのではなくて、対象のリソースが処理できなかったことを明確に伝えるために、個別の例外処理をすることは、十分考えられます。

このように、正常なビジネス処理において起こりうる例外については、そもそもビジネス的にどのように対応するか仕様を明確にして、個別に対応してあげた方が好ましいものも多いでしょう。

逆にたとえば、サーバーサイドのAPIを利用しようとした場合、通信状態が悪ければ例外が発生するでしょう。これらは個別に扱わず、必要であれば適当なリトライ処理の上で、特別な処理は行わずにシステムエラーとしてしまった方が良いでしょう。

- 業務シナリオとして起こりうるケースの判定に、例外を用いる必要がある場合は個別処理をする。
- 業務シナリオとは関係なく、システム的な要因による例外は、例外ハンドラーで共通処理をする。

おおまかな方針としては、こんな感じが好ましいと考えています。

ここでは共通の例外ハンドラーの扱いについて設計していきましょう。

### 例外ハンドリングの初期化

今回は画面処理フレームワークにKamishibaiをもちいて、WPFアプリケーションはGeneric Host上で動作させます。

そのため、例外ハンドリングの初期化はつぎのように行います。

```cs
var builder = KamishibaiApplication<TApplication, TWindow>.CreateBuilder();

// 各種DIコンテナーの初期化処理

var app = builder.Build();
app.Startup += SetupExceptionHandler;
await app.RunAsync();
```

ビルドしたappのStartupイベントをフックして、アプリケーションが起動した直後にSetupExceptionHandlerを呼び出して、例外ハンドリングを初期化します。

SetupExceptionHandlerの中では、つぎの3つのハンドラーを利用して例外処理を行います。

1. Application.Current.DispatcherUnhandledException
2. AppDomain.CurrentDomain.UnhandledException
3. TaskScheduler.UnobservedTaskException

### Application.Current.DispatcherUnhandledException

具体的な実装はつぎの通りです。

```cs
Application.Current.DispatcherUnhandledException += (sender, args) =>
{
    Log.Warning(args.Exception, "Dispatcher.UnhandledException sender:{Sender}", sender);
    // 例外処理の中断
    args.Handled = true;

    // システム終了確認
    var confirmResult = MessageBox.Show(
        "システムエラーが発生しました。作業を継続しますか？",
        "システムエラー",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning,
        MessageBoxResult.Yes);
    if (confirmResult == MessageBoxResult.No)
    {
        Environment.Exit(1);
    }
};
```

例外情報をログに出力したあと、例外処理を中断します。

その後に、システムの利用を継続するかどうか、ユーザーに確認を取り、継続が選ばれなかった場合はアプリケーションを終了します。

WPFの例外ハンドラーは、基本的には[Application.DispatcherUnhandledException](https://learn.microsoft.com/ja-jp/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-8.0)で例外を処理します。Application.DispatcherUnhandledExceptionでは例外チェーンを中断できますが、それ以外では中断できないためです。

Environment.Exit(1)を呼び出さなくても、最終的にはアプリケーションは終了します。しかし、Environment.Exit(1)を呼び出さないと、つづいてAppDomain.CurrentDomain.UnhandledExceptionが呼び出されます。例外の2重処理になりやすいため、明示的に終了してしまうのが好ましいでしょう。

### AppDomain.CurrentDomain.UnhandledException

先のApplication.DispatcherUnhandledExceptionでは、つぎのように、明示的に作成したThreadで発生した例外は補足できません。

```cs
var thread = new Thread(() =>
{
    throw new NotImplementedException();
});
thread.Start();
```

この場合は、[AppDomain.UnhandledException](https://learn.microsoft.com/ja-jp/dotnet/api/system.appdomain.unhandledexception?view=net-7.0)を利用して例外を補足します。

AppDomain.CurrentDomain.UnhandledExceptionでは、つぎのようにログ出力の後に、ユーザーにエラーを通知してアプリケーションを終了します。AppDomain.CurrentDomain.UnhandledExceptionでは例外チェーンを中断できず、この後アプリケーションはかならず終了されるため、確認はせずに通知だけします。

```cs
AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    Log.Warning(args.ExceptionObject as Exception, "AppDomain.UnhandledException sender:{Sender}", sender);
    
    // システム終了通知
    MessageBox.Show(
        "システムエラーが発生しました。作業を継続しますか？",
        "システムエラー",
        MessageBoxButton.OK,
        MessageBoxImage.Error,
        MessageBoxResult.OK);

    Environment.Exit(1);
};
```

このとき、Environment.Exit(1)を呼び出すことで、Windowsのアプリケーションのクラッシュダイアログの表示を抑制します。

### TaskScheduler.UnobservedTaskException

つぎのようにTaskをasync/awaitせず、投げっぱなしでバックグラウンド処理した際に例外が発生した場合は、[TaskScheduler.UnobservedTaskException](https://learn.microsoft.com/ja-jp/dotnet/api/system.threading.tasks.taskscheduler.unobservedtaskexception?view=net-7.0)で補足します。

```cs
private void OnClick(object sender, RoutedEventArgs e)
{
    Task.Run(() =>
    {
        throw new NotImplementedException();
    });
}
```

ただTaskScheduler.UnobservedTaskExceptionは例外が発生しても即座にコールされないため注意が必要です。

ユーザーの操作とは無関係に、「いつか」発行されるため、ユーザーに通知したり、アプリケーションを中断しても混乱を招くだけです。

未処理の例外は全般的に、あくまで最終手段とするべきものですが、とくにTaskScheduler.UnobservedTaskExceptionは最後の最後の保険と考えて、つぎのようにログ出力程度に留めておくのが良いでしょう。

```cs
TaskScheduler.UnobservedTaskException += (sender, args) =>
{
    Log.Warning(args.Exception, "TaskScheduler.UnobservedTaskException sender:{Sender}", sender);
    args.SetObserved();
};
```

SetObservedは .NET Framework 4以前はアプリケーションが終了してしまうことがありましたが、現在は呼ばなくても挙動は変わらないはずです。一応念のため呼び出しています。

## gRPCの例外処理アーキテクチャ

Web APIで何らかの処理を実行中に例外が発生した場合、通常はリソースを解放してログを出力するくらいしかできません。ほかにできる事といえば、外部リソース（たとえばデータベース）を利用中に例外が発生したのであればリトライくらいでしょうか？

特殊な事をしていなければリソースの解放はC#のusingで担保するでしょうし、解放漏れがあったとしても例外時にフォローすることも難しいです。そのため実質的にはログ出力くらいです。

MagicOnionを利用してgRPCを実装する場合、通常はASP.NET Core上で開発します。ASP.NET Coreで開発していた場合、一般的なロギングライブラリであれば、APIの例外時にはロガーの設定に則ってエラーログは出力されることが多いでしょう。

では何もする必要がないのでしょうか？

そんなことはありません。Web APIの実装側でも別途ログを出力しておくべきです。これはASP.NET Coreレベルでのログ出力では、接続元のアドレスは表示できても、たとえば認証情報のようなデータはログに出力されない為です。誰が操作したときの例外なのか、障害の分析には最重要情報の1つです。ASP.NET Coreレベルのログも念のため残しておいた方が安全ですが、アプリケーション側ではアプリケーション側で例外を出力しましょう。

MagicOnionを利用してこのような共通処理を組み込みたい場合、認証のときにも利用したMagicOnionFilterAttributeを利用するのが良いでしょう。

このとき認証のときに利用したフィルターに組み込んでも良いのですが、つぎのように認証用のフィルターの後ろに例外処理用のフィルターを配置した方が良いと考えています。

![](Article03/スライド25.PNG)

これは認証とログ出力は別の関心だからです。そう関心の分離ですね。ログ出力を修正したら認証が影響を受けてしまった。またはその逆のようなケースを防ぐためには、別々に実装しておいて組み合わせた方が良いでしょう。

具体的な実装はつぎの通りです。

```cs
public class ExceptionFilterAttribute : MagicOnionFilterAttribute
{
    private readonly ILogger<ExceptionFilterAttribute> _logger;
    private readonly IAuthenticationContext _authenticationContext;

    public ExceptionFilterAttribute(
        ILogger<ExceptionFilterAttribute> logger, 
        IAuthenticationContext authenticationContext)
    {
        _logger = logger;
        _authenticationContext = authenticationContext;
    }

    public override ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
    {
        try
        {
            return next(context);
        }
        catch (Exception e)
        {
            // 例外情報をログ出力した後に再スローする。
            _logger.LogError(・・・・
            throw;
        }
    }
}
```

ILoggerとIAuthenticationContextをDIコンテナーから注入することで、認証情報を活用したログ出力が可能となります。

具体的なログ出力については、つぎの章で詳細を設計しましょう。