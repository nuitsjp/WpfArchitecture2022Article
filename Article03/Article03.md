2022年版実践WPF業務アプリケーションのアーキテクチャ【設計編　設計】 ～ ドメイン駆動設計＆Clean Architectureとともに ～

# リード文（400字以内）

先日、「[2022年版実践WPF業務アプリケーションのアーキテクチャ【設計編　前編】～ドメイン駆動設計＆Clean Architectureとともに～](https://codezine.jp/article/detail/16953)」という記事を公開させていただきました。こちらの記事では、初期のドメインビュー・ユースケースビューから、アーキテクチャを設計していくための土台となる初期の論理ビュー・実装ビュー・配置ビュー・データビュー・プロセスビューの設計を行い、全体の大まかな設計を行いました。その結果、個別の非機能要件やユースケースを個別に設計できる状態になりました。

今回は、非機能要件とユースケースから代表的なものを選定して詳細を蒸留します。最後に開発者ビューで、利用者側の要件からは出てきにくい、開発上に必要となる設計、テスト設計などを実施します。

最終的に非機能要件が満たされた状態で、代表的なユースケースの基本シナリオを実現し、テストが実装できるところまでを説明します。本稿を完走することで、同一パターンのユースケースの実装が並行に行える状態となります。

では、設計を始めましょう！

# 前提条件

本稿はWPFアプリケーションのアーキテクチャ設計について記載したものです。すでに公開されている「見積編」および「設計編　前編」が前提となります。未読なものがあれば、そちらからまずはお読みください。

- [見積編](https://codezine.jp/article/detail/16953)
- [設計編　前編](https://codezine.jp/article/detail/16953)


本稿にはサーバーサイドの設計も一部含まれていますが、見積編にも記載した通り、サーバーサイドについてはWPFアプリケーションを設計する上で、必要最低限の範囲に限定しています。サーバーサイドの実現方式は、オンプレ環境なのかクラウド環境なのか？といった容易などで大きく変わってきます。そしてWPFアプリケーションから見た場合には本質的な問題ではありません。サーバーサイドまで厳密に記載すると話が発散し過ぎてしまい、WPFアプリケーションのアーキテクチャにフォーカスすることが難しくなるため、あくまで参考程度にご覧ください。

また本稿ではAdventureWorks社の業務のうち、発注業務システムのアーキテクチャとなります。特定業務は発注業務に限定されますが、認証などの複数の業務にまたがったアーキテクチャの実現についても言及しています。

本稿は以下の環境を前提に記載しています。  

* Visual Studio 2022 Version 17.4.0
* Docker Desktop 4.14.0
* Docker version 20.10.20
* SQL Server 2022-latest(on Docker)
* [ComponentOne for WPF Edition 2022v2](https://www.grapecity.co.jp/developer/componentone/wpf)
* [SPREAD for WPF 4.0J](https://www.grapecity.co.jp/developer/spread-wpf)
* Test Assistant Pro 1.123
* .NET 6.0.11

本稿のサンプルは .NET 6で構築しますが、.NET Framework 4.6.2以上（.NET Standard 2.0水準以上）であれば同様のアーキテクチャで実現可能です。ただし一部利用しているパッケージのバージョンを当てなおす必要があるかもしれません。

# 想定読者

次の技術要素の基本をある程度理解していることを想定しています。

* C#  
* WPF  
* Docker
* SQL Server

これらの基本的な解説は、本稿では割愛しますが、知らないと理解できないという訳でもありません。

また下記の2つも概要は理解できていることが好ましいです。

* Clean Architecture
* ドメイン駆動設計（DDD）

Clean Architectureについては、筆者のブログである「[世界一わかりやすいClean Architecture](https://www.nuits.jp/entry/easiest-clean-architecture-2019-09)」をあわせて読んでいただけると、本稿のアーキテクチャの設計意図が伝わりやすいかと思います。

ドメイン駆動設計の適用範囲については、本文内でも、つど解説いたします。

# 本稿の構成

本稿はつぎの構成で進めます。

1. 非機能要件の実現
   1. 認証アーキテクチャ
   2. 例外処理アーキテクチャ
   3. ロギングアーキテクチャ
2. 代表的なユースケースの実現
   1. 「再発注する」ユースケースの実現
3. 開発者ビューの設計
   1. ユニットテスト設計
   2. 継続的インテグレーション設計

# 非機能要件の実現

本章では非機能要件の中でも需要が高く、設計初期に共通で実施しておく必要のあるつぎの2つの非機能要件を例に、アーキテクチャを設計します。

1. 認証アーキテクチャ
2. 例外・ロギングアーキテクチャ

例外とロギングは厳密には異なる要件ではありますが、密接に関連するため、並行で設計を行います。

またログ出力をする場合、一般的に利用者情報を負荷することが多いと思います。そのため、先に認証処理を実現するとむだが少ないため、さきに認証処理の設計を行う事とします。

# 認証アーキテクチャの設計

では認証アーキテクチャを設計していきましょう。それにあたり、いったん中心となるドメインを購買ドメインから認証ドメインに移します。

以下の図が購買ドメインを設計してきた、現時点の境界付けられたコンテキストです。

![](Article03/スライド1.PNG)

これはあくまで、購買ドメインを中心に見たモデルです。そのため、購買ドメインにとって重要度が低い部分は、意図的に省略してきました。

ここからしばらくは、認証の設計をしていくため、認証ドメインを中心に設計します。認証ドメインは、購買・製造・販売それぞれから「汎用」ドメインとして共有されるドメインとなります。そのため、個別のドメインの中で設計するよりは、認証ドメインを独立した設計書として設計していくのが良いかと思います。

## 認証ドメインからみた境界付けられたコンテキスト

さて、認証ドメインから境界付けられたコンテキストはつぎの通りです。

![](Article03/スライド2.PNG)

さすがに認証ドメイン視点とはいえ、認証ドメインがコアドメインになったりはせず、支援ドメインのままでしょう。

ただしコアドメインは、購買ドメインではなく、販売ドメインとなるでしょう。営利企業なので販売が最優先だと判断しました。

また認証ドメインと他のドメインの関係に焦点を絞って設計しています。

認証ドメインは、購買・製造・販売ドメインから共有される汎用ドメインで、それらから依存されています。

また認証ドメインでは、たとえばログインユーザーなどの文脈は、AdventureWorksコンテキストの文脈を利用します。そのためAdventureWorksドメインに依存します。

認証処理では登録ユーザーを確認する必要があるでしょうから、汎用データベースドメインにも依存するでしょう。

このように視点が変わるとドメインの見え方は変わってきます。ドメインごとに境界付けられたコンテキストを定義することで、個々の境界付けられたコンテキストの複雑性を下げます。統合されたたった1つのモデルは、大きなドメインでは複雑すぎる場合があります。

また、すべての視点から正しい、たった1つのモデルを作るってメンテナンスし続けることは困難です。一定規模に分割して設計していくのが好ましいと考えています。

## 認証ドメインからみたコンテキストマップ

さて、これらをコンテキストマップとして記載したものが次の通りです。

![](Article03/スライド5.PNG)

認証とデータベースがいずれも汎用コンテキストなので、それらとの関係は、カスタマー・サプライヤーとし、AdventureWorksとは共有カーネルの関係とします。

## 認証の背景と要件

今回は、業務アプリケーションということで、つぎのような背景があるものとします。

1. 利用者はAdventure Works社の従業員である
2. 利用者はWindowsドメイン参加しているWindows OSから利用する
3. 利用者の勤務時間管理に、Windowsの起動・停止時間を利用している
4. 十分な休憩を挟んで勤務しているか、停止から起動のインターバルを参照して管理している

そのため、認証は非機能定義書において、つぎのような要件として定められているものとします。

1. Windows認証にてアカウントを特定する
2. 特定されたアカウントが従業員として登録されていれば利用可能とする
3. APIの呼出し時には、つど認証情報を検証する
4. 認証の有効期限は24時間とする
5. 期限を超えた場合、再認証する
6. 再認証はアプリケーションの再起動で行う

今回開発する対象は購買管理のシステムであり、日中の業務となり、日をまたいで継続した利用は通常運用では考えていません。また長時間認証された状態が維持されることはセキュリティ上好ましくありません。24時間は実用上長いですが、12時間では短く、その中間に適切な時間もないため24時間を有効期限とします。

## 認証方式の選択

gRPCで利用可能な認証方式は、次のようなものがあります。

1. Azure Active Directory
2. クライアント証明書
3. Identityサーバー
4. JSON Web Token
5. OAuth 2.0
6. OpenID Connect
7. WS-Federation

- [ASP.NET Core のための gRPC での認証と承認](https://learn.microsoft.com/ja-jp/aspnet/core/grpc/authn-and-authz?view=aspnetcore-7.0#other-authentication-mechanisms)

Windows認証が含まれていません。

実はgRPCでは直接Windows認証は利用できません。これは、gRPCがプラットフォームに依存しないHTTP/2プロトコルとTLSに基づいているためです。これにより、Windows認証のような特定のプラットフォームに依存する認証メカニズムはサポートされません。

ただこれは、gRPCで直接Windows認証が行えないというだけで、Windows認証の併用ができない訳ではありません。

すこし「メタ」な話になりますが、本稿では認証基盤は本質的な課題ではありません。また誰もがすぐに動かして試せる範囲に収めたいため、Azure Active DirectoryやIdentityサーバーなどは避けたいです。そこで今回はJSON Web Token（JWT）を活用して、Windows認証の認証情報をgRPCで利用します。

JWTを利用した認証方式は、オンプレのような閉じた環境でも利用しやすく、gRPCと組み合わせやすい特徴があります。Windows認証を適用したREST APIでJWTを作成し、gRPCで利用することで、gRPCでもWindows認証で認証されたトークンを利用できます。

JSON Web Token（JWT）は、デジタル署名されたJSONデータ構造で、認証と認可情報を交換するために使用されます。JWTは3つのパートで構成されており、それぞれBase64Urlエンコードされた形式で、ドット（.）で区切られています。これらのパートは、ヘッダー、ペイロード、署名です。

```txt
例：
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

ヘッダー（Header）: トークンのタイプと使用する署名アルゴリズムを指定します。
例: {"alg": "HS256", "typ": "JWT"}

ペイロード（Payload）: クレームと呼ばれる情報（ユーザーID、有効期限など）を含むJSONデータです。
例: {"sub": "1234567890", "name": "John Doe", "iat": 1516239022}

署名（Signature）: ヘッダーとペイロードを結合し、秘密鍵を使ってデジタル署名を生成します。これにより、トークンの内容が改ざんされていないことが検証できます。

JWTを使用すると、クライアントとサーバー間で認証情報を安全かつ効率的に交換できます。ただし、機密データは含めないように注意してください。

## 認証処理の概略

さてJWTはトークンの仕様であり、認証されたトークンが有効なものかどうかを検証する仕組みしか実はありません。どのようにWindows認証を行い、どうトークンを作成するかまでは含まれていません。

トークンはサーバーサイドで、秘密鍵を用いて何らかの方法で発行する必要があります。

ノード・コンポーネント・鍵の配置は、つぎのようになるでしょう。

![](Article03/スライド3.PNG)

そのうえで、つぎのように振舞います。

![](Article03/スライド4.PNG)

購買アプリケーションは、起動時にRESTの認証APIを呼び出します。認証APIはRESTなので、Windows認証して呼び出し元の利用者のユーザーIDを取得できます。

取得したユーザーIDを認証データベースと照らし合わせて、利用者情報を取得します。

データベースにユーザーが無事登録されていたら、利用者情報からトークンを作成して秘密鍵で署名して、トークンを購買アプリに返却します。

購買アプリは、署名されたトークンをHTTPヘッダーに付与して購買APIを呼び出します。購買APIでは、トークンを公開鍵で複合して検証し、問題なければAPIの利用を許可します。

トークンには発行日時を含められるため、利用期限を設けることができますし、任意の情報を付与できるため、購買APIを利用するために必要なユーザー情報（たとえば従業員IDなど）をトークンに含めておくこともできます。都度ユーザー情報を取得するようなオーバーヘッドを避けられます。

また購買API上だけでなく、購買アプリ上でも公開鍵を用いて復号することで、トークンに含まれた情報を利用できます。

あたり前ですが、非常に良くできた仕組みですね。

## 論理ビュー設計

さて、これらの検討結果から、論理ビューを設計してみましょう。そこんな感じでしょうか？

![](Article03/スライド6.PNG)

最外周にUIがありません。認証ドメインは、他のドメインに認証機能を提供するドメインのため、UIが存在しないからです。

その代わりにWindows認証を行うためのWeb API（REST）があります。

またユースケースレイヤーもありません。認証ドメインにも「ユースケース（利用シーンの意味）」はあります。認証と検証ですね。ただ、ユースケース単位のオブジェクトは必要ないと考えたので、ユースケースレイヤーは利用しません。

こんな感じで、外周やレイヤーなど、必要に応じて取捨選択したり、必要なものを追加します。クリーンアーキテクチャの一般的なレイヤー構成や構成要素に限定して考える必要はまったくありません。

さて登場オブジェクトを見てみましょう。

|レイヤー|オブジェクト|説明|
|--|--|--|
|AdventureWorksドメイン|User|システムの利用者|
||IUserRepository|Userのリポジトリー|
|認証ドメイン|IAuthenticationService|認証処理を実施し、IAuthenticationContextを初期化する|
||IAuthenticationContext|認証されたユーザーを扱う、認証コンテキスト|
|コントローラー・ゲートウェイ|UserSerializer|UserとJWTのシリアライズ・デシリアライズを行う|
||AuthenticationServiceClient|REST APIを呼び出して認証処理を行う|
||AuthenticationController|REST APIを提供し、Windows認証からユーザーを特定して利用者を認証する|
||IClientAuthenticationContext|IAuthenticationContextを継承した、gRPC用の認証コンテキスト。IAuthenticationContextと異なり、JWTトークンを保持して、gRPCを呼び出す際にサーバーサイドにトークンを渡す。|
||AuthenticationFilter|gRPCのクライアントを呼び出すと、必ず通過するフィルター。IClientAuthenticationContextからトークンを取得してヘッダーに付与することで認証情報をサーバーサイドに渡す。|
||ServerAuthenticationContext|IAuthenticationContextのgRPCサーバーサイド実装。クライアント側は1プロセス1ユーザーだが、サーバーサイドは1プロセスマルチユーザーのため、異なる実装が必要となる。|
||AuthenticationFilterAttribute|gRPCのサーバーサイドが呼び出された場合に必ず通過するフィルター。リクエストヘッダーからトークンを取得し、UserSerializerで複合することでユーザーの検証を行う。|

さきにも少し触れましたが、認証ドメインではざっくり言うと、認証と検証の2種類のユースケースがあります。認証はRESTで、検証はgRPCの利用時に行います。

そのため、上図のオブジェクトはつぎの2つのユースケースで考えると分かりやすいです。

1. REST APIによるユーザーの認証処理
2. gRPC利用時のユーザーの検証処理

RESTで認証された際に作られたJWTを利用して、gRPCを呼出し、正しく認証されたユーザーにだけgRPC APIの利用を許可します。順番に見ていきましょう。

### REST APIによる認証処理

RESTによる認証は、WPFアプリケーションの起動時に実施します。だいたいつぎのような流れになります。検証処理側は除外しています。

![](Article03/スライド7.PNG)

アプリケーションの起動時に、最初の画面のロードイベントで認証サービスを呼び出します。

認証は最初のスプラッシュ画面やローディング画面を表示した後に実施します。画面表示前に実施しておいて、認証情報をDIコンテナーに登録してしまうのがもっとも簡単です。ただその場合、初期画面の表示に時間が掛かってしまいます。そのため、初期画面を表示しておいて、初期画面で認証処理を行います。

ほかにはアプリケーション本体の画面とは別に、何らかの方法でスプラッシュを表示しておいて、認証し、認証情報をDIコンテナーに登録する方法もありだと思います。ただ今回は、初期画面で処理するようにしています。

では実際にコードも見ながら説明していきましょう。

初期画面の遷移時にIAuthenticationServiceを呼び出して、サーバーサイドの認証サービスを呼び出します。

```cs
private readonly IAuthenticationService _authenticationService;

public async Task OnNavigatedAsync(PostForwardEventArgs args)
{
    var result = await _authenticationService.TryAuthenticateAsync();
    if (result.IsAuthenticated is false)
    {
        _presentationService.ShowMessage(
            "ユーザー認証に失敗しました。",
            "認証エラー",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        // アプリケーションを終了する。
        Environment.Exit(1);
    }
}
```

認証に失敗したら、アラートを表示した後にシステムを強制的に終了します。

AuthenticationServiceのTryAuthenticateAsyncでは、HttpClientを利用して認証RESTサービスを呼び出します。

```cs
/// <summary>
/// Windows認証を有効化したHTTPクライアント
/// </summary>
private static readonly HttpClient HttpClient = new(new HttpClientHandler { UseDefaultCredentials = true });

private readonly Audience _audience;

public async Task<AuthenticateResult> TryAuthenticateAsync()
{
    try
    {
        var baseAddress = Environments.GetEnvironmentVariable(
            "AdventureWorks.Authentication.Jwt.Rest.BaseAddress",
            "https://localhost:4001");
        var token = await HttpClient.GetStringAsync($"{baseAddress}/Authentication/{_audience.Value}");
        _context.CurrentTokenString = token;
        _context.CurrentUser = UserSerializer.Deserialize(token, _audience);
        return new(true, Context);
    }
    catch
    {
        return new(false, Context);
    }
}
```

認証APIにaudienceを渡しています。JWTのaudience（audクレーム）は、トークンの受信者を特定するために使用されるます。購買ドメインでは、購買APIサービスを呼び出します。この購買APIサービスがトークンの受信者になります。

認証処理を呼び出すさい、購買APIサービスを表すaudクレームを渡します。

なおAPIのアドレス（https://foo.co.jpなど）は、実運用や各種テスト環境、実装環境すべてで異なります。その問題を解決する何らかの方法が必要です。個人的には環境変数を好んでいます。設定ファイルに記述する方法もありますが、その場合、ビルドしたモジュールに含まれる設定ファイルを環境別に書き換える必要があって、トラブルになりがちです。モジュールはバイナリーレベルでつねに完全に一致する形で扱うのが好みです。そこで環境依存の値を環境変数から取得することで、その問題を避ける方法をよく利用します。

さて、AuthenticationServiceを利用すると、ASP.NET CoreのAuthenticationControllerが呼び出されます。下記のコードが呼び出されるメソッドです。

```cs
private readonly IUserRepository _userRepository;

[HttpGet("{audience}")]
public async Task<string> AuthenticateAsync(string audience)
{
    var account = User.Identity!.Name!;
    if (await _userRepository.TryGetUserByIdAsync(new LoginId(account), out var user))
    {
        // ここで本来はuserとがaudienceを照らし合わせて検証する

        // 認証が成功した場合、ユーザーからJWTトークンを生成する。
        return UserSerializer.Serialize(user, Properties.Resources.PrivateKey, new Audience(audience));
    }

    throw new AuthenticationException();
}
```

ASP.NET Coreでは、Windows認証を有効にしておくと「User.Identity!.Name!」から、簡単に呼び出し元のindowsアカウントを特定できます。

アカウントを取得したら、IUserRepositoryインターフェイル経由でUserRepositoryを呼び出してUserオブジェクトを取得することでユーザーを認証します。

その後、何らかの形でuserとaudienceを照らし合わせて、audienceを利用できるか検証（認可）します。

問題なければ、UserSerializerを利用して認証されたUserをJSON Web Token（JWT）にシリアライズします。この時秘密鍵で署名することで、公開鍵でトークンが正しいものであることを検証できるようにします。JWTはレスポンスとして返却します。

![](Article03/スライド8.PNG)

AuthenticationServiceでは正しいレスポンスが帰ってきたら、認証が正しく行われたものと判断します。

ここでJWTを公開鍵をもちいて複合することで、認証情報の詳細を取得できます。JWTには任意の情報を詰めることができますが、あまり情報を詰めすぎると、gRPCの呼出し時に通信量が増えてしまいます。

今回はJWTには従業員IDだけ詰めることにしましたが、ロールのような権限情報を付与しても良いと思います。

### gRPC利用時のユーザーの検証処理

さて、続いてはアプリケーション操作時にgRPCを呼び出した際の検証処理です。

![](Article03/スライド9.PNG)

ユーザーが購買アプリケーションで何らかの操作をすると、ViewModelはgRPCのクライアント経由でサーバーサイドを呼び出します。

この時、gRPCクライアントにAuthenticationFilterを適用してJWTトークンをHTTPヘッダーに付与します。

IAuthenticationContextはUserと、認証済のトークンを保持しています。

```cs
public interface IAuthenticationContext
{
    /// <summary>
    /// 認証済ユーザーを取得する。
    /// </summary>
    User CurrentUser { get; }

    /// <summary>
    /// 認証済みトークンを取得する。
    /// </summary>
    public string CurrentTokenString { get; }
}
```

gRPC呼び出し時に、HTTPヘッダーへ認証済みトークンを付与します。

```cs
public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
{
    var header = context.CallOptions.Headers;
    header.Add("authorization", $"Bearer {_authenticationContext.CurrentTokenString}");

    return await next(context);
}
```

authorizationにBearer～の形式でトークンを設定するのは、OAuthの仕組みに則っています。

トークンはHTTPヘッダーに格納されて、メッセージとともにリモートへ送信します。

サーバーサイドでgRPCが呼び出された場合、リクエストをいったんすべてAuthenticationFilterAttributeで受け取り、トークンを検証します。

```cs
private readonly ServerAuthenticationContext _serverAuthenticationContext;

public override async ValueTask Invoke(ServiceContext context, Func<ServiceContext, ValueTask> next)
{
    try
    {
        var entry = context.CallContext.RequestHeaders.Get("authorization");
        var token = entry.Value.Substring("Bearer ".Length);
        _serverAuthenticationContext.CurrentUser = UserSerializer.Deserialize(token, _audience);
        _serverAuthenticationContext.CurrentTokenString = token;
    }
    catch (Exception e)
    {
        _logger.LogWarning(e, e.Message);
        context.CallContext.GetHttpContext().Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    try
    {
        await next(context);
    }
    finally
    {
        _serverAuthenticationContext.ClearCurrentUser();
    }
}
```

リクエストヘッダーのauthorizationからJWTを取得します。

取得したトークンをUserSerializer.Deserializeをつかって署名を検証しつつ複合し、ServerAuthenticationContextに設定することで、以後必要に応じて利用します。

サーバーサイドではIAuthenticationContextをDIすることで、インスタンスを使いまわす想定です。単純にプロパティに設定してしまうと、他者の権限で実行されてしまう可能性があります。そのため、サーバー用のIAuthenticationContextはつぎのように実装しています。

```cs
public class ServerAuthenticationContext : IAuthenticationContext
{
    private readonly AsyncLocal<User> _currentUserAsyncLocal = new();

    public User CurrentUser
    {
        get
        {
            if (_currentUserAsyncLocal.Value is null)
                throw new InvalidOperationException("認証処理の完了時に利用してください。");

            return _currentUserAsyncLocal.Value;
        }

        internal set => _currentUserAsyncLocal.Value = value;
    }
}
```

実体はAsyncLocal&lt;T>に保持します。これによって同一スレッド上ではかならず同じユーザーが取得できます。また設定はフィルターを通して行い、設定できた場合のみgRPCの実際の処理が実行されるます。

あとは必要な箇所でIAuthenticationContextをDIコンテナーから注入して利用します。

```cs
public class VendorRepositoryService : ServiceBase<IVendorRepositoryService>, IVendorRepositoryService
{
    private readonly IVendorRepository _repository;

    private readonly IAuthenticationContext _authenticationContext;

    public VendorRepositoryService(IVendorRepository repository, IAuthenticationContext authenticationContext)
    {
        _repository = repository;
        _authenticationContext = authenticationContext;
    }

    public async UnaryResult<Vendor> GetVendorByIdAsync(VendorId vendorId)
    {
        // 呼び出し元のユーザー情報を利用する。
        var user = _authenticationContext.CurrentUser;

        return await _repository.GetVendorByIdAsync(vendorId);
    }
}
```
# 例外処理アーキテクチャ

つづいて例外処理アーキテクチャについて設計します。例外処理は、WPFとgRPCでまったく異なります。そのため、それぞれ個別に設計していきましょう。

## WPFの例外処理アーキテクチャ

WPFの例外処理は、特別な意図がある場合を除いて、標準で提供されている各種の例外ハンドラーで一括して処理することにします。

実際問題、起こりうる例外をすべて正しく把握して、個別に設計・実装することはそもそも現実味がありません。特定の例外のみ発生個所で個別に例外処理をしても、全体としての一貫性が失われることが多いです。また、例外の隠ぺいや必要なログ出力のもれにつながりやすいです。であれば、グローバルな例外ハンドラー系に基本的には任せて一貫した例外処理をまずは提供するべきかと思います。

ただもちろんすべてを否定するわけではありません。

たとえば、何らかのファイル処理をするときにファイルのロックを取得するために、書き込みモードでオープンしておく、なんてことはありがちだと思います。このとき、事前に別のプロセスにファイルが開かれていた場合は、例外が発生するでしょう。そのような場合には、ロック処理で例外をキャッチして、ロックできなかったことをユーザーに伝えるべきでしょう。

このように、正常なビジネス処理において起こりうる例外については、個別に対応してあげた方が好ましいものも多いでしょう。

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

Web APIで何らかの処理を実行中に例外が発生した場合、通常はリソースを解放してログを出力するくらいしかできません。ほかにで切る事といえば、外部リソース（たとえばデータベース）を利用中に例外が発生したのであればリトライくらいでしょうか？

特殊な事をしていなければリソースの解放はC#のusingで担保するでしょうから、実質的にはログ出力くらいです。

MagicOnionを利用してgRPCを実装する場合、通常はASP.NET Core上で開発します。ASP.NET Coreで開発していた場合、一般的なロギングライブラリであれば、APIの例外時にはロガーの設定に則ってエラーログは出力されることが多いでしょう。

では何もする必要がないのでしょうか？

そんなことはありません。Web APIの実装側でも別途ログを出力しておくべきです。これはASP.NET Coreレベルでのログ出力では、たとえば認証情報のようなデータはログに出力されない為です。誰が操作したときの例外なのか、障害の分析には最重要情報の1つです。ASP.NET Coreレベルのログも念のため残しておいた方が安全ですが、アプリケーション側ではアプリケーション側で例外を出力しましょう。

MagicOnionを利用してこのような共通処理を組み込みたい場合、認証のときにも利用したMagicOnionFilterAttributeを利用するのが良いでしょう。

このとき認証のときに利用したフィルターに組み込んでも良いのですが、つぎのように認証用のフィルターの後ろにログ出力用のフィルターを配置した方が良いと考えています。

![](Article03/スライド10.PNG)

これは認証とログ出力は別の関心だからです。そう関心の分離ですね。ログ出力を修正したら認証が影響を受けてしまった。またはその逆のようなケースを防ぐためには、別々に実装しておいて組み合わせた方が良いでしょう。

具体的な実装はつぎの通りです。

```cs
public class LoggingFilterAttribute : MagicOnionFilterAttribute
{
    private readonly ILogger<LoggingFilterAttribute> _logger;
    private readonly IAuthenticationContext _authenticationContext;

    public LoggingFilterAttribute(
        ILogger<LoggingFilterAttribute> logger, 
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
