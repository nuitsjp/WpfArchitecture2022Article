# 開発者ビュー

さて、ここまでは開発する対象のアーキテクチャを設計してきました。ここからは、開発対象の周囲の環境を中心とした開発者ビューを設計します。

1. バージョン管理戦略
2. コーディング規約
3. テスト戦略
4. ビルド戦略
5. デプロイ戦略
6. リリース戦略

これらすべてがアーキテクチャかというと、コーディング規約など、あまり一般的ではないものも含まれます。ただ私は「アーキテクチャとは技術的に重要な決定のすべて」とう論を推しています。そのため、開発者ビューもアーキテクチャの一部として設計することにしています。

開発の各フェーズを想定して、他の文書で規定されない技術的な決定は、少なくともいったんは開発者ビューに入れてしまってよいと考えています。必要になったときに、個別の文書などに切り出すことを検討します。

## 概要を設計する

上記の要素を設計していくにあたって、多くの要素は独立しているわけではなくて、複雑に絡み合っています。そのため、ざっくり概要を設計した上で詳細に落とし込んでいきます。

またそのためには、開発の背景が必要になってきます。短いサイクルでリリースし続けるシステムなのか、短くても3か月～半年くらいのサイクルでリリースされるシステムなのか。どの程度クリティカルなシステムなのか。などなど、開発の背景を把握しておく必要があります。

そこで本稿では、つぎのような背景を想定して設計を進めていきます。

> 開発対象のシステムは、業務上重要なシステムであり、基幹システムとして認定されていて、それにふさわしい開発品質を保証する必要がある。
> 開発対象のシステムは、3か月～半年くらいのサイクルでリリースされる。重要度の低い不具合は、次期リリースのタイミングでリリースされるが、重要度の高い不具合は、緊急リリースとしてリリースされることもある。ただし緊急リリースの頻度は十分に低い。
> システムの利用者と開発者は異なる組織であり、開発者は通常、システムが運用されるネットワークとは異なる環境で開発している。
> 開発サイドのテストが完了した後、運用環境に持ち込み、運用環境と同一ネットワーク上にある受入テスト環境でテストを行い、問題なければ本番環境にリリースする。

上記のような背景があるためCIは実施しますが、CDに関してはビルドしたモジュールをビルドモジュールの配置場所に配置するところまでを行うこととします。手動テストの実施は、配置場所からテスト環境につどデプロイして実施します。その際、可能な限りスクリプトによってデプロイする方針とします。これによって、受入テストや本番環境へのリリースに対するテストが、常時開発環境で行われることとなり、リリース品質の向上が見込めます。

では個別の要素について設計していきましょう。

## バージョン管理戦略

バージョン管理システムにはGitHubを使います（あくまで本稿での決定で、GitHubがもっともよいという意味合いではありません）。

近年のAIの急速な普及や、それらのエコシステムへの取り組み事情を考えたときに、GitHubを利用することで品質や生産性を最大化できると考えました。

さて、Gitを利用する場合、バージョン管理のブランチ戦略を考える必要があるでしょう。Gitのブランチ戦略として一般的によく知られているものとして、Gitflow Workflow、GitHub Flow、およびGitLab Flowの3つが挙げられます。

AdventureWorksではGitflow Workflowをを採用することとします。

Gitflow Workflowは、大規模なプロジェクトや複数の開発者が関与するプロジェクトに適しています。この戦略では、以下のような役割ごとのブランチが利用されます。

- main：本番環境用で、リリース済みのコードが保管されるブランチです。
- develop：開発用ブランチで、機能追加やバグ修正のコミットが行われる場所です。
- feature：新機能開発用のブランチで、developブランチから派生し、開発が完了したらdevelopにマージされます。
- release：リリース前の最終調整用のブランチで、developから派生し、準備が整ったらmasterとdevelopにマージされます。
- hotfix：緊急のバグ修正用のブランチで、masterから派生し、修正が完了したらmasterとdevelopにマージされます。

他の2つのブランチ戦略との違いの1つにdevelopブランチの有無があります。リリースサイクルが長めの開発では、mainブランチとdevelopブランチが分かれていると扱いやすいと考えています。

GitLab Flowのmainブランチとenvironmentブランチの関係も似ていますが、受入テストが完了したモジュールを、そのまま本番環境に昇格する運用としたいため、Gitflow Workflowを採用することとします。

なお私個人は、Gitflow Workflowのreleaseブランチを省略した形で運用しているケースが多いです。mainブランチでリリースモジュールも作りこんでいく形で運用しています。

## コーディング規約

開発上利用する言語のコーディング規約は、早い段階で決めておくことが望ましいです。コーディング規約を0から書き上げるのは非常に大変な作業です。そこで、既存のコーディング規約を参照しつつ、必要に応じて変更を加えていくことをオススメします。

たとえばC#であれば、私はMicrosoftの公式コーディング規約をベースにしています。

- [Microsoft C# のコーディング規則](https://learn.microsoft.com/ja-jp/dotnet/csharp/fundamentals/coding-style/coding-conventions)

ただしprivateまたはinternalであるstaticフィールドを使用する場合の、「s_」プレフィックスと、スレッド静的なプレフィックスの「t_」は受け入れがたいため、これらのプレフィックスを使用しないようにしています。

そういった形で、一般的なコーディング規約をベースに、マッチしない部分だけをカスタマイズしています。

## テスト戦略

WPFのテスト戦略を考えたとき、アーキテクチャとして重要になるのは、どこまでを自動テストとして含めるか？という点です。

WPFを操作した場合、概ねつぎのようなフローとなります。

1. ユーザーがViewを操作する
2. ViewがViewModelのコマンドを呼び出す。またはバインドされたプロパティを更新する
3. ViewModelがユースケースもしくはドメイン層をよびだす
4. 呼び出されたドメイン層の実体はgRPCのクライアントで、サーバーサイドを呼び出す
5. サーバーサイドでドメイン層の実体が実行される

このとき、自動テストとしてどこまでを含めるべきでしょうか？選択肢としては現実的につぎの2つが考えられます。

1. ViewModelの振る舞いまでを含める
2. サーバーサイドの振る舞いまで含める

基本的に前者の方が低コストで実施できて、後者の方がテスト価値は高いです。

後者まで実現できれば、システムのリグレッションテストとして利用できるため、テストは非常に高い価値をもたらしてくれるでしょう。しかし、実際には非常に多くの課題を解決する必要がでてきます。

現実的には、サーバーサイドの実装までほぼ完成した段階にならないと、テストも完成しないことになります。結合テストまで完了した状態にならないと、テストも完成しませんし、多くのプロジェクトではそのタイミングでテストに十分に投資するコストも時間も残っていない事が経験上多いです。

これを解決するためには、プロジェクトの受託前から顧客とも、システムを結合した状態でのテストを維持するコストを支払い続けられるか、入念に話し合っておく必要があります。

また後者はある程度テストに習熟した組織でないと運用は難しいでしょう。前者をまずは導入してみて、十分な経験を得てから後者に取り組んでいくというのも現実的な選択肢です。

さて、WPF単体のアーキテクチャの視点で考えると、じつは前者のケースの方が考えるべきことが多いです。というのは、後者のケースはシステムとしてはほぼ完成していて、テストのためのアーキテクチャとして考えることは、テストケースごとにテストデータをどう入れ替えるか？くらいだからです。

逆に前者の場合、ViewModelより下の層をスタブとして入れ替える仕組みが必要になります。しかもテストケースによってスタブの振る舞いは変える必要があるため、そのアーキテクチャを決定する必要があります。

今回、予算編でも説明した通り、WPFのテストはOSSのテストフレームワークであるFriendlyと、その有償サポートツールのTest Assistant Proを利用します。

UIのテストフレームワークは多数ありますが、一般的なキャプチャー＆リプレイ方式のツールは、テストを維持するのが非常に厳しいと感じてきました。何らかの修正が入ったとき、テストを再度キャプチャーしなおさないといけないのは、非常に手間です。

それに対してFriendlyをつかったテストコードは非常にメンテナンス性が高いです。

下記のコードは、ユースケースの実現で記載したシナリオ「再発注する」をテストするコードです。

```cs
[Test]
public void 再発注する()
{
    var mainWindow = _app.AttachMainWindow();
    var menuPage = _app.AttachMenuPage();
    var rePurchasingPage = _app.AttachRePurchasingPage();
    var requiringPurchaseProductsPage = _app.AttachRequiringPurchaseProductsPage();

    ////////////////////////////////////////////////////////////////////////////
    // MenuPage
    ////////////////////////////////////////////////////////////////////////////
    // メニューから再発注を選択し、RequiringPurchaseProductsPageへ移動する。
    menuPage.NavigateRePurchasing.EmulateClick();
    mainWindow.NavigationFrame.Should().BeOfPage<RequiringPurchaseProductsPage>();


    ////////////////////////////////////////////////////////////////////////////
    // RequiringPurchaseProductsPage
    ////////////////////////////////////////////////////////////////////////////
    // グリッドの表示行数の確認
    requiringPurchaseProductsPage.RequiringPurchaseProducts.RowCount.Should().Be(9);
    // 選択済みベンダーの確認
    requiringPurchaseProductsPage.SelectedRequiringPurchaseProductVendorName.Text.Should().Be("Vendor 1");

    // 発注ボタンを押下し、RePurchasingPage画面へ遷移する。
    requiringPurchaseProductsPage.PurchaseCommand.EmulateClick();
    mainWindow.NavigationFrame.Should().BeOfPage<RePurchasingPage>();

    ////////////////////////////////////////////////////////////////////////////
    // RePurchasingPage
    ////////////////////////////////////////////////////////////////////////////
    // 発注ボタンを押下し、登録完了ダイアログで、OKを押下する。
    var async = new Async();
    rePurchasingPage.PurchaseCommand.EmulateClick(async);
    var messageBox = _app.Attach_MessageBox(@"");
    messageBox.Button_OK.EmulateClick();
    async.WaitForCompletion();

    // RequiringPurchaseProductsPage画面へ戻る
    mainWindow.NavigationFrame.Should().BeOfPage<RequiringPurchaseProductsPage>();

    ////////////////////////////////////////////////////////////////////////////
    // RequiringPurchaseProductsPage
    ////////////////////////////////////////////////////////////////////////////
    // 発注した商品が減っていることを確認する。
    requiringPurchaseProductsPage.RequiringPurchaseProducts.RowCount.Should().Be(7);
}
```

Friendlyを知らない方でも、何をやっているかだいたい理解できるコードになっているかと思います。Friendlyの詳細はここでは説明を省略します。公式のGitHubをご覧ください。

- [https://github.com/Codeer-Software/Friendly](https://github.com/Codeer-Software/Friendly)

なおFriendlyとTest Assistant Proを作成しているのは日本の会社なので、サポートや導入コンサルティングを日本語で受けられるのも大きなポイントです。

さてFriendlyではテストを実施するときに、テスト対象のWPFアプリケーションを別プロセスで起動して、そこにアタッチする形でテストを実行します。

```cs
var mainWindow = _app.AttachMainWindow();
```

さて、購買管理WPFアプリケーションのエントリーポイントはAdventureWorks.Business.Purchasing.Hosting.Wpfです。テストのときにこれを呼び出すと、バックエンドにつながってしまいます。

そのためテスト用のスタブに切り替えてあげる必要があります。しかもテストケースごとにスタブが入れ替えられると便利でしょう。

そこでテスト用のエントリーポイント用のプロジェクトを作成しましょう。そしてそこでスタブを切り替えるようにします。

Friendlyでテストアプリケーションを起動するとき、つぎのようにテスト用のエントリーポイントを相対パスで指定できます。指定するのはビルドされたexeになります。

```cs
public static WindowsAppFriend Start()
{
    //target path
    var targetPath = @"..\..\..\..\AdventureWorks.Purchasing.App.Driver\bin\Debug\net6.0-windows\AdventureWorks.Purchasing.App.Driver.exe";
    var info = new ProcessStartInfo(targetPath) { WorkingDirectory = Path.GetDirectoryName(targetPath)! };
    var app = new WindowsAppFriend(Process.Start(info));
    app.ResetTimeout();
    return app;
}
```

ここで起動されるプロセス環境変数にテスト名を設定して起動します。

```cs
var info = new ProcessStartInfo(targetPath) { WorkingDirectory = Path.GetDirectoryName(targetPath)! };
info.Environment["TestName"] = context.Test.FullName;
```

テストのFullNameには先の例だと「Scenario.RePurchasingTest.再発注する」が設定されます。

これを起動されたテスト用のエンドポイントで読み取ってスタブを切り替えます。

```cs
string? testName = Environment.GetEnvironmentVariable("TestName");
```

testNameからifやcaseで分岐しても良いのですが、テストが増えてくると分岐の数が大変なことになります。

そこでスタブをDIコンテナーに登録するための共通処理を表すIContainerBuilderインターフェイスを作成します。

```cs
public interface IContainerBuilder
{
    void Build(IServiceCollection services);
}
```

このインターフェイスの実装クラスを、テストケースごとにテスト名の名前空間に作成します。

```cs
namespace AdventureWorks.Purchasing.App.Driver.Scenario.RePurchasingTest.再発注する;

public class ContainerBuilder : IContainerBuilder
{
    public void Build(IServiceCollection services)
    {
        // 認証サービスを初期化する。
        services.AddTransient<IAuthenticationService, AuthenticationService>();
        services.AddTransient<IAuthenticationContext, AuthenticationContext>();
        ・・・・
```

そしてテストのエントリーポイントで、テスト名の名前空間からIContainerBuilderを動的に生成して、Buildメソッドを呼び出します。

```cs
var builder = ApplicationBuilder<App, MainWindow>.CreateBuilder();

try
{
    string testName = Environment.GetEnvironmentVariable("TestName")!;
    var builderName = $"AdventureWorks.Purchasing.App.Driver.{testName}.ContainerBuilder";
    var builderTye = Type.GetType(builderName)!;
    var builderInstance = Activator.CreateInstance(builderTye) as IContainerBuilder;
    builderInstance!.Build(builder.Services);
    ・・・
```

これでテストケースごとにスタブを切り替えることで、たとえば要発注対象一覧画面の表示データを切り替えることが可能となります。

### ビルド戦略

ブランチ戦略で記載したように、開発中はPull Requestはdevelopブランチに対して行われます。developブランチはデプロイできる状態を可能な限り保ちます。

そのため、可能であればつぎのようなビルド戦略を取りたいと思います。

1. Pull RequestをトリガーにCIパイプラインが走ること
2. CIではビルドとテストを実施すること
3. テストはWPFにおいてはテスト戦略で記載した自動テストを実施すること

バージョン管理システムがGitHubなので、CIパイプラインはGitHub Actionsを利用します。GitHub ActionsでWPFアプリケーションのビルドやUI自動テストを実施する方法は、次の記事で紹介しています。参考にどうぞ。

- [.NETのWPFアプリケーションをGithub Actionsでビルドする](https://zenn.dev/nuits_jp/articles/2022-07-04-net-wpf-build-with-actions)
- [Friendlyで作ったWPFの自動テストをGithub Actions上でテストする（.NET Framework編）](https://zenn.dev/nuits_jp/articles/2022-07-09-net-framework-and-friendly-on-actions)

この際、気を付けておかないといけないことの1つに、GitHub Actionsの実行環境があります。

WPFの自動テストを実施しようとした場合、ユーザーインターフェイスを実行する必要があることから、ユーザープロセスでのテストが必要になります。そのためWindowsにログインした環境で、GitHubのテストエージェントをユーザープロセスで実行しておく必要があります。

またUIのテストは非常に時間がかかります。そのため規模が大きくなってきた場合、CIパイプラインの並列化を検討する必要があります。一般的にはクラウド上のVMなどを利用したくなりますが、Pull Requestがいつ来るか分からない中で、常にVMを起動しておくのはコストがかかります。

一番現実的なプランは、「使わなくなったパソコンを大量に確保しておいて、テストエージェントのプールを作る事」だったりすることも多いです。

またいくら並列化しても、場合によってはPull Requestが更新されるたびにCIを流すことは現実的ではなくなってくる可能性があります。その場合はブランチ戦略を含め、開発者ビュー全体を見直す必要があるかもしれません。

### デプロイ戦略

ビルドが終わったらデプロイします。

ただ前述した通り、デプロイ戦略といってもクラウド上に展開するといった意味合いではありません。今回は、ビルドされたWPFアプリケーションやそのインストーラーを、ビルドバージョンごとに保管することを指します。実際にはWPFアプリケーションだけではなく、サーバーサイドや、データベース構築スクリプトなど、同一バージョンは一式セットで保管すると良いでしょう。

この時、コスト的な観点から、私はAzure BLOB Storageに保管しています。

### リリース戦略

今回の背景から、リリースは3種類のリリースを想定します。

1. 開発環境におけるテストリリース（結合・システムテスト）
2. ユーザー受入テスト向けのリリース
3. 本番リリース

ただし、モジュールは完全に同じものをもちいて、かつ、可能な限りスクリプトで自動化します。

ここまでの中でも触れてきましたが、環境によらず、基本的に実行モジュールはバイナリーレベルで完全に一致する形で運用したいと考えています。

環境に依存するものは環境変数で制御して、インストーラーや環境構築時に一度だけ設定するようにします。

こうすることで、リリース誤りを最小限に抑えることができます。



