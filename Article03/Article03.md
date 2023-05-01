2022年版実践WPF業務アプリケーションのアーキテクチャ【設計編　設計】 ～ ドメイン駆動設計＆Clean Architectureとともに ～

# リード文（400字以内）

先日、「[2022年版実践WPF業務アプリケーションのアーキテクチャ【設計編　前編】～ドメイン駆動設計＆Clean Architectureとともに～](https://codezine.jp/article/detail/16953)」という記事を公開させていただきました。こちらの記事では、初期のドメインビュー・ユースケースビューから、アーキテクチャを設計していくための土台となる初期の論理ビュー・実装ビュー・配置ビュー・データビュー・プロセスビューの設計を行い、全体の大まかな設計を行いました。その結果、個別の非機能要件やユースケースを個別に設計できる状態になりました。

今回は、非機能要件とユースケースから代表的なものを選定して詳細を蒸留します。最後に開発者ビューで、利用者側の要件からは出てきにくい、開発上に必要となる設計、テスト設計などを実施します。

最終的に非機能要件が満たされた状態で、代表的なユースケースの基本シナリオを実現し、テストが実装できるところまでを説明します。本稿を完走することで、同一パターンのユースケースの実装が並行に行える状態となります。

では、設計を始めましょう！

# ソースコード

実際に動作するソースコードは、GitHub上に公開しています。ぜひご覧ください。

- [https://github.com/nuitsjp/WpfArchitecture2022](https://github.com/nuitsjp/WpfArchitecture2022)

ビルドや実行方法については、リンク先のREADME.mdをご覧ください。

また実際に動作させるためにはつぎの2つのライセンスが必要です。

* [ComponentOne for WPF Edition 2022v2](https://www.grapecity.co.jp/developer/componentone/wpf)
* [SPREAD for WPF 4.0J](https://www.grapecity.co.jp/developer/spread-wpf)

これらは試用ライセンスを発行していただくことができます。

T.B.D.　正式な試用ライセンスの申告方法をここに記載する

本稿は、本稿だけで読み進められるように記載していますが、すべてのコードを詳細に解説しているわけではありません。本稿を読んだ後、あらためて動作させつつコードと本稿を読み比べていただければ、理解が深まるかと思います。

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
# 非機能要件の実現

本章では非機能要件の中でも需要が高く、設計初期に共通で実施しておく必要のあるつぎの3つの非機能要件を例に、アーキテクチャを設計します。

1. 認証アーキテクチャ
2. 例外処理アーキテクチャ
3. ロギングアーキテクチャ

ログ出力をする場合、一般的に利用者情報を付加することが多いと思います。そのため、先に認証処理を実現するとむだが少ないため、さきに認証処理の設計を行う事とします。

# 認証アーキテクチャの注意点

**インターネットのように広く公開するサービスの場合は、適切な認証プロバイダーを選定してください。**

本稿では、実際に皆さんにコードを動かしていただくことを想定して、外部サービスを利用しないで実現する方式を選定しています。企業内ネットワークのような限定された環境では必要十分な設計だと思いますが、オープンに公開するためには認証機能を独自開発するようなリスクは避けるべきでしょう。また仮に企業内ネットワークであっても、ほかに利用できる認証プロバイダーがあるのであれば、そちらの利用をご検討ください。

たとえばAzure Active Directoryのような製品を利用することを検討してください。

# 認証アーキテクチャの設計

では認証アーキテクチャを設計していきましょう。それにあたり、いったん中心となるドメインを購買ドメインから認証ドメインに移します。

さて、詳細の前に注意点があります。

以下の図が購買ドメインを設計してきた、現時点の境界付けられたコンテキストです。

![](Article03/スライド1.PNG)

これはあくまで、購買ドメインを中心に見たモデルです。そのため、購買ドメインにとって重要度が低い部分は、意図的に省略してきました。

ここからしばらくは、認証の設計をしていくため、認証ドメインを中心に設計します。認証ドメインは、購買・製造・販売それぞれから「汎用」ドメインとして共有されるドメインとなります。そのため、個別のドメインの中で設計するよりは、認証ドメインを独立した設計書として設計していくのが良いかと思います。

## 認証ドメインからみた境界付けられたコンテキスト

さて、認証ドメインから境界付けられたコンテキストはつぎの通りです。

![](Article03/スライド2.PNG)

さすがに認証ドメイン視点とはいえ、認証ドメインがコアドメインになったりはせず、汎用ドメインのままでしょう。

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

|パート|説明|
|--|--|
|ヘッダー（Header）|トークンのタイプと使用する署名アルゴリズムを指定します。<br>例: {"alg": "HS256", "typ": "JWT"}|
|ペイロード（Payload）|クレームと呼ばれる情報（ユーザーID、有効期限など）を含むJSONデータです。<br>例: {"sub": "1234567890", "name": "John Doe", "iat": 1516239022}|
|署名（Signature）|ヘッダーとペイロードを結合し、秘密鍵を使ってデジタル署名を生成します。これにより、トークンの内容が改ざんされていないことが検証できます。|

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

## 認証ドメインの論理ビュー設計

さて、これらの検討結果から、論理ビューを設計してみましょう。そこんな感じでしょうか？

![](Article03/スライド6.PNG)

UserSerializerがレイアウトの都合上、右上の左下の2カ所に配置していますがご了承ください。

まず最外周にUIがありません。認証ドメインは、他のドメインに認証機能を提供するドメインのため、UIが存在しないからです。

その代わりにWindows認証を行うためのWeb API（REST）があります。

またユースケースレイヤーもありません。認証ドメインにも「ユースケース（利用シーンの意味）」はあります。認証と検証ですね。ただ、ユースケース単位のオブジェクトは必要ないと考えたので、ユースケースレイヤーは利用しません。

こんな感じで、外周やレイヤーなど、必要に応じて取捨選択したり、必要なものを追加します。クリーンアーキテクチャの「例の図」にあるレイヤー構成や構成要素に限定して考える必要はまったくありません。

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
||ClientAuthenticationContext|IAuthenticationContextを実装した、gRPC用の認証コンテキスト。IAuthenticationContextと異なり、JWTトークンを保持して、gRPCを呼び出す際にサーバーサイドにトークンを渡す。|
||AuthenticationFilter|gRPCのクライアントを呼び出すと、必ず通過するフィルター。ClientAuthenticationContextからトークンを取得してヘッダーに付与することで認証情報をサーバーサイドに渡す。|
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

では実際ながれを追っていきましょう。

①初期画面の遷移時にIAuthenticationServiceを呼び出します。

IAuthenticationServiceの実体はAuthenticationServiceで、HttpClientを利用して、②サーバーサイドの認証サービスを呼び出します。ここのAPIは前述のとおりRESTです。

サーバーサイドではAuthenticationControllerがリクエストを受け取り、③Windows認証を使ってアカウントを特定し、④IUserRepositoryを利用して、アカウントに対応する適切なUserか判定します。

![](Article03/スライド8.PNG)

適切なユーザーであれば、⑤UserSerializerを利用して認証されたUserをJSON Web Token（JWT）にシリアライズします。この時秘密鍵で署名することで、公開鍵でトークンが正しいものであることを検証できるようにします。JWTはレスポンスとして返却します。

AuthenticationServiceではレスポンスからトークンを受け取り、⑥トークンを復元してユーザー情報をClientAuthenticationContextへ設定します。

### gRPC利用時のユーザーの検証処理

さて、続いてはアプリケーション操作時にgRPCを呼び出した際の検証処理です。

![](Article03/スライド9.PNG)

①ユーザーが購買アプリケーションで何らかの操作をすると、ViewModelはgRPCのクライアント経由でサーバーサイドを呼び出します。

この時、gRPCクライアントにAuthenticationFilterを適用して②JWTトークンをHTTPヘッダーに付与します。

gRPCのサーバーサイドでは、AuthenticationFilterAttributeが受け取ったリクエストのヘッダーのauthorizationからJWTを取得します。取得したトークンを③UserSerializer.Deserializeをつかって複合します。

ただしく複合できたら、④ServerAuthenticationContextに設定することで、以後必要に応じて利用します。

概ね悪くなさそうですね。では実際にコードを書きつつ、実装ビューを作って詳細に設計を落としていきましょう。
## 認証ドメインの実装ビュー設計

さてでは、先ほどのオブジェクトをコンポーネント単位に振り分けてみましょう。

![](Article03/スライド10.PNG)

こんな感じでしょうか？

認証ドメインのトップレベルのオブジェクトであるIAuthenticationServiceとIAuthenticationContextをAdventureWorks.Authenticationコンポーネント（つまりVisual Studioのプロジェクト）とします。

認証はJSON Web Token（JWT）で実現します。そのため署名・復元をおこなうUserSerializerをAdventureWorks.Authentication.Jwtコンコーネんとに配置します。

JWTのクライアント側の実装となるAuthenticationServiceとClientAuthenticationContextをAdventureWorks.Authentication.Jwt.Clientに、サーバー側実装となるAuthenticationControllerをAdventureWorks.Authentication.Jwt.Serverに配置します。

WPFのケースでも説明しましたが、ホスティングに関する実装はそれだけに分離したいため、ASP.NET CoreのエントリーポイントとなるProgramクラスはAdventureWorks.Authentication.Jwt.Hosting.Restプロジェクトに置きました。

![](Article03/スライド11.PNG)

論理ビューとの対比はこんな感じ。とくに抜け漏れはなさそうです。

つづいて、リモートのビジネスロジックを呼び出して検証する側を見ていきましょう。

![](Article03/スライド12.PNG)

検証側はMagicOnionを利用したgRPC呼び出しになります。

gRPCでJWTを利用するために、クライアント側でトークンをHTTPヘッダーに登録するAuthenticationFilterは、AdventureWorks.Authentication.MagicOnion.Clientに配置しました。

同様にサーバーサードで検証を行うAuthenticationFilterAttributeは、AdventureWorks.Authentication.MagicOnion.Serverに配置しました。

検証時のシーケンスはこんな感じで、UserSerializerは1つだけ配置したので少しレイアウトが違いますが、だいたい同じようになりました。

もう一度全体を眺めてみましょう。

![](Article03/スライド10.PNG)

依存関係に循環もなく、コンポーネント間も基本的にインターフェイスベースの結合となっていて、悪くなさそうです。

では本当に問題ないか、仮実装しながら設計を検証していきましょう。

### 認証処理の実装による検証

まずはアプリケーション起動直後の認証処理です。MainWindowのViewModelに認証処理を組み込んで、認証が通ればメニューを表示するように実装します。

そのため、IAuthenticationServiceと画面遷移を提供するIPresentationServiceをDIコンテナーから注入します。

```cs
private readonly IAuthenticationService _authenticationService;
private readonly IPresentationService _presentationService;

public MainViewModel(
    [Inject] IAuthenticationService authenticationService,
    [Inject] IPresentationService presentationService)
{
    _authenticationService = authenticationService;
    _presentationService = presentationService;
}
```

KamishibaiではViewModelにDIコンテナーから注入したいオブジェクトにはInject属性を宣言する仕様となっています。

Kamishibaiでは型安全かつNullableを最大限活用して画面遷移パラメーターを渡せるように、コンストラクターで受け取れるようになっています。その際に、画面遷移パラメーターと、DIコンテナーから注入するオブジェクトを区別するために、注入する側にInject属性を付与する仕様になっています。

とはいえここでは、Kamishibaiのお作法は「だいたいこんなものかな」という理解で問題ありません。

その上で、MainWindowの画面遷移完了後に認証処理を呼び出します。

```cs
public class MainViewModel : INavigatedAsyncAware
{
    ・・・

    public async Task OnNavigatedAsync(PostForwardEventArgs args)
    {
        var result = await _authenticationService.TryAuthenticateAsync();
        if (result.IsAuthenticated)
        {
            await _presentationService.NavigateToMenuAsync();
        }
        else
        {
            _presentationService.ShowMessage(
                Purchasing.ViewModel.Properties.Resources.AuthenticationFailed,
                Purchasing.ViewModel.Properties.Resources.AuthenticationFailedCaption,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            // アプリケーションを終了する。
            Environment.Exit(1);
        }
    }
}
```

Kamishibaiでは画面遷移後に処理を行いたい場合、INavigatedAsyncAwareを実装し、OnNavigatedAsyncで通知をうけます。

コンストラクターから注入したIAuthenticationServiceのTryAuthenticateAsyncを呼び出してユーザーを認証し、認証エラーとなった場合、アラートを表示してアプリケーションを終了します。

ViewModel上の処理は問題なさそうです。

ではTryAuthenticateAsyncの実装を確認しましょう。

```cs
// Windows認証を有効化したHTTPクライアント
private static readonly HttpClient HttpClient = new(new HttpClientHandler { UseDefaultCredentials = true });
private readonly ClientAuthenticationContext _context;  // DIコンテナーから注入する
private readonly Audience _audience;                    // DIコンテナーから注入する

public async Task<AuthenticateResult> TryAuthenticateAsync()
{
    try
    {
        // 環境変数からAPIのエンドポイントを取得する。
        var baseAddress = Environments.GetEnvironmentVariable(
            "AdventureWorks.Authentication.Jwt.Rest.BaseAddress",
            "https://localhost:4001");
        // 認証処理を呼び出す。
        var token = await HttpClient.GetStringAsync($"{baseAddress}/Authentication/{_audience.Value}");
        // トークンを受け取って複合し、結果をAuthenticationContextへ設定する。
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

APIのベースアドレス（https://foo.co.jp など）は、実運用や各種テスト環境、実装環境すべてで異なります。その問題を解決する何らかの方法が必要で、個人的には環境変数を好んでいます。設定ファイルに記述した場合、ビルドしたモジュールに含まれる設定ファイルを環境別に書き換える必要があるため、トラブルになりがちだからです。

"AdventureWorks.Authentication.Jwt.Rest.BaseAddress"が環境変数の名称になります。環境変数名と一緒にデフォルト値を渡しています。開発時は、クローンしてビルドしただけで、そのまま実行できることが好ましいです。そのため、開発環境は環境変数がない前提でデフォルト値を渡しています。

認証APIには引数としてAudienceを渡しています。JWTのaudience（audクレーム）は、トークンの受信者を特定するために使用されるます。購買ドメインでは、購買APIサービスを呼び出します。この購買APIサービスがトークンの受信者になります。そのため認証時に購買APIサービスのAudienceを渡します。

認証が正しくおこなれたら、DIコンテナーから注入されたClientAuthenticationContextにユーザー情報を反映します。ClientAuthenticationContextはシングルトンにして、認証情報を必要とする箇所でシングルトンインスタンスを注入して利用します。

ではサーバー側のコードを確認してみましょう。

```cs
private readonly IUserRepository _userRepository;

[HttpGet("{audience}")]
public async Task<string> AuthenticateAsync(string audience)
{
    var account = User.Identity!.Name!;
    var user = await _userRepository.GetUserAsync(new LoginId(account));
```

ASP.NET Coreでは、Windows認証を有効にしておくと「User.Identity!.Name!」から、簡単に呼び出し元のWindowsアカウントを特定できます。

アカウントを取得したら、IUserRepositoryインターフェイル経由でUserRepositoryを呼び出してUserオブジェクトを取得することでユーザーを認証します。

IUserRepositoryの実装クラス、UserRepositoryの実装を見てみましょう。

```cs
    public async Task<User?> GetUserAsync(LoginId loginId)
    {
        using var connection = _database.Open();

        const string query = @"
select
	EmployeeId
from
	AdventureWorks.vUser
where
	LoginId = @LoginId
";
        return await connection.QuerySingleOrDefaultAsync<User>(
            query,
            new
            {
                LoginId = loginId
            });
    }
```

一般的なDapperの実装です。定数定義されたクエリーを実行し、実行結果をDapperを利用して自動的にUserオブジェクトに値を設定します。

もう少し深堀して見てみましょう。Userクラスの中身を見てみましょう。

```cs
public record User(EmployeeId EmployeeId);

[UnitOf(typeof(int))]
public partial struct EmployeeId{}
```

Userはrecord型のオブジェクトで、ドメイン駆動型設計のエンティティに該当します。

UserはメンバーにEmployeeIdを持っています。EmployeeIdは構造体で、ドメイン駆動設計のバリューオブジェクトに該当します。

EmployeeIdはint型で扱うこともできるのですが、IDの取り違いはありがちな不具合を発生しがちです。

つぎのコードはEmployeeIdとProductIdをintであつかった時のサンプルコードです。

```cs
public record ProductOrder(int ProductId, int EmployeeId);

public void Order(int employeeId, int productId)
{
    var productOrder = new ProductOrder(employeeId, productId);
```

ProductOrderにProductIdとEmployeeIdを渡していますが、順序が逆になってしまっています。そしてこのコードはコンパイルが通ってしまします。

もちろん適切なコードがあれば、いずれかのタイミングで気が付きます。しかしデータベースから値を取得したときに、テストデータの初期値はどちらも1だったりすると、気が付くのが遅くなってしまうこともままあります。

ではIDをバリューオブジェクトとして扱った場合はどうなるでしょうか？

```cs
public record ProductOrder(ProductId ProductId, EmployeeId EmployeeId);

public void Order(EmployeeId employeeId, ProductId productId)
{
    var productOrder = new ProductOrder(employeeId, productId);
}
```

このコードはコンパイルエラーになるので、実装時に即座にエラーに気が付きますし、そもそもIDEが適切なコードをアシストしてくれるかもしれません。

私は、開発上で最初のテストはコンパイルであると思っています。コンパイルはもっともはやく、かならず実行され、そしてテストを間違いません。そのため実装スタイルと一番重要な鉄則の1つに

「不具合をコンパイラーが捕捉できるコードを優先する」

があると思っていて、IDをバリューオブジェクトとして扱うことは、ベストプラクティスの1つだと思っています。

さてEmployeeIdをもう一度見てみましょう。

```cs
[UnitOf(typeof(int))]
public partial struct EmployeeId{}
```

UnitOf属性が付与されていることが見て取れますが、バリューオブジェクトの実装には[UnitGeneratorライブラリ](https://github.com/Cysharp/UnitGenerator)を利用します。

IDはもっとも単純なバリューオブジェクトですが、金額や重量のような計算をともなう場合は、実装が複雑になりがちです。UnitGeneratorは非常によく考えられたライブラリで、ドメイン駆動設計を強力にサポートしてくれるのでオススメです。

さて、実は下記のDapperを利用したコードはこのままでは動作しません。

```cs
return await connection.QuerySingleOrDefaultAsync<User>(
    query,
    new
    {
        LoginId = loginId
    });
```

EmployeeIdをDapperが解釈できないからです。そのため、つぎのようなTypeHandlerを用意してあげる必要があります。

```cs
public class EmployeeIdTypeHandler : SqlMapper.TypeHandler<EmployeeId>
{
    public override void SetValue(IDbDataParameter parameter, EmployeeId value)
    {
        parameter.DbType = DbType.Int32;
        parameter.Value = value.AsPrimitive();
    }

    public override EmployeeId Parse(object value)
    {
        return new EmployeeId((System.Int32)value);
    }
}
```

UnitGeneratorにはこのTypeHandlerをつぎのように宣言するだけ実装できます。

```cs
[UnitOf(typeof(int), UnitGenerateOptions.DapperTypeHandler)]
public partial struct EmployeeId
{
}
```

よくできていますね。よくできているんですが、UnitGenerator側ではなくて、システム全体のアーキテクチャとしては少し問題があります。全体の構造を見てみましょう。

![](Article03/スライド10.PNG)

EmployeeIdはUserオブジェクトと同じようにAdventureWorksコンポーネントに配置されます。そのため、上記のように宣言的にTypeHandlerを実装しようとした場合、AdventureWorksがDapperに依存してしまいます。

もちろん、アーキテクチャ的な決断として、AdventureWorksがDapperに依存するのを受け入れるという手もあります。

ただ個人的にはあまり好みではありません。というのは、AdventureWorksがDapperに依存してしまった場合、Dapperのバージョンを上げないといけないとなったときに、ほぼすべてのドメインが影響を受けてしまうからです。Dapperのバージョンを気軽に上げるということが、かなわなくなります。

ではUnitGeneratorは良いのか？というと、受け入れられる範囲だと思っています。UnitGeneratorは、Valueオブジェクトを生成するライブラリという側面ではすでに完成されていて、なんならバージョンはほぼ永久的に固定することができそうです。またUnitGeneratorはValueオブジェクトのコードを自動生成しているだけなので、問題があれば手動での実装に切り替えても支障がありません。

少しメタなことをいうと、その方がアーキテクチャ的に複雑なので、ここで解説するためという意図もあります。

そのため本稿ではそこは妥協せず、TypeHandlerを作成して、AdventureWorks.SqlServer側に配置することとしました。

![](Article03/スライド15.PNG)

AdventureWorksにEmployeeIdを、AdventureWorks.SqlServerにEmployeeIdTypeHandlerを配置しました。

EmployeeIdTypeHandlerの実装ですが、UnitGeneratorで宣言的に解決しないとなると、みずから実装しなくてはなりません。すべてのValueObjectに対して実装するのはそれなりに手間なので、コード生成形の手段で解決したいところです。

今回はT4 Templateを利用して、つぎのように解決することにしました。

```cs
<#
	var @namespace = "AdventureWorks.SqlServer";
	var types = new []
	{
		(UnitName: "EmployeeId", UnitType: typeof(int)),
        ・・・
	};
#>

<#@ include file="..\AdventureWorks.Database\DapperTypeHandlers.t4" once="true" #>
```

T4の詳細は割愛します。少し古い仕組みですが、C#でもっとも簡単に利用できるコード生成手段です。生成されたコードがバージョン管理できるところが、個人的には結構好きです。

includeディレクティブで指定している[DapperTypeHandlers.t4は直接GitHubで](https://github.com/nuitsjp/WpfArchitecture2022/blob/main/Source/AdventureWorks.Database/DapperTypeHandlers.t4)コードをみてみてください。

さて、忘れないうちに購買ドメインの配置ビューも更新しておきましょう。

![](Article03/スライド16.PNG)

UnitGeneratorはサーバーサイド、クライアントサイドのどちらにも配置されます。

さぁ、これでDapperを利用してエンティティやバリューオブジェクトを直接利用できるようになりました。ということで、認証処理側に戻りましょう。

```cs
[HttpGet("{audience}")]
public async Task<string> AuthenticateAsync(string audience)
{
    var account = User.Identity!.Name!;
    var user = await _userRepository.GetUserAsync(new LoginId(account));
    if (user is null)
    {
        throw new AuthenticationException();
    }

    // ここで本来はuserとがaudienceを照らし合わせて検証する

    // 認証が成功した場合、ユーザーからJWTトークンを生成する。
    return UserSerializer.Serialize(user, Properties.Resources.PrivateKey, new Audience(audience));
}
```

IUnitRepositoryからUserを取得して、取得できなかった場合、ユーザーとして登録されていない為、認証エラーとします。

その後、何らかの形でuserとaudienceを照らし合わせて、audienceを利用できるか検証（認可）します。

ユーザーとオーディエンスの情報がそろうことで、ユーザーの特定だけでなく、そのユーザーが対象のオーディエンスを利用できるかどうか、認可することが可能になります。

ここで秘密鍵で署名することで、認証情報を持ったJSON Web Token（JWT）を作成します。

JWTには任意の情報を詰めることができますが、あまり情報を詰めすぎると、gRPCの呼出し時に通信量が増えてしまいます。今回はJWTには従業員IDだけ詰めることにしましたが、ロールのような権限情報を付与しても良いと思います。

さてこれで、サーバーサイドの処理が終わったのでクライアント側に戻ります。

```cs
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
        return new(true, _context);
    }
    catch
    {
        return new(false, _context);
    }
}
```

サーバーサイドでAuthenticationExceptionがスローされると、クライアント側でも例外が発生するので、キャッチして認証エラーとします。利用者が認証できないケースは、機能的なシナリオとして十分考えられることで、ここではランタイムエラーとはせずに、例外はキャッチして通常のロジック内で処理します。

正常に返却された場合、秘密鍵で証明されたトークンが返却されるので、トークンと、トークンから複合したUserオブジェクトを保持します。

トークンはgRPCの通信時に利用し、Userオブジェクトは必要に応じてアプリケーションで利用します。

これで認証全体の流れが実装できることが確認できました。

記事内では結構すんなり進んでいますが、記事を書くために実装している間は、だいぶモデルとコードを行ったり来たりして、何度も細かい設計変更を行っています。10箇所やそこらじゃないです。「そういうもの」だと思ってください。
### 検証処理の実装による検証

さて、続いてはアプリケーション操作時にgRPCを呼び出した際の検証処理です。

![](Article03/スライド13.PNG)

ユーザーが購買アプリケーションで何らかの操作をすると、ViewModelはgRPCのクライアント経由でサーバーサイドを呼び出します。

ちょっとこのままだと、具体的な実装が見えにくいので、前回の「設計編　全編」で購買ドメインのVendorオブジェクトをIVendorRepository経由で取得するオブジェクトを配置してみましょう。また手狭になってしまうので、認証側のオブジェクトをいったん削除したもでるがつぎの図です。

![](Article03/スライド14.PNG)

ではViewModeから順番にコードを追って実装を確認していきましょう。

ユーザーが何らかの操作をしたとき、ViewModeにDIされたIVendorRepositoryを呼び出してVendorオブジェクトを取得します。

```cs
private readonly IVendorRepository _vendorRepository;

private async Task PurchaseAsync()
{
    var vendor = await _vendorRepository.GetVendorByIdAsync(_selectedRequiringPurchaseProduct!.VendorId);
```

このとき実際には、IVendorRepositoryを実装したVendorRepositoryClientが呼び出されます。

```cs
private IAuthenticationContext _authenticationContext;
private Endpoint _endpoint;

public async Task<Vendor> GetVendorByIdAsync(VendorId vendorId)
{
    var server = MagicOnionClient.Create<IVendorRepositoryService>(
        GrpcChannel.ForAddress(_endpoint.Uri),
        new IClientFilter[]
        {
            new AuthenticationFilter(_authenticationContext)
        });
    return await server.GetVendorByIdAsync(vendorId);
}
```

MagicOnionClientからIVendorRepositoryServiceのインスタンスを動的に生成して、サーバーサイドを呼び出します。Endpointは初出ですが、これは後ほど説明します。

IVendorRepositoryServiceを生成するときにAuthenticationFilterを適用します。

AuthenticationFilterではつぎのように、認証時に取得したトークンをHTTPヘッダーに付与します。

```cs
public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
{
    var header = context.CallOptions.Headers;
    header.Add("authorization", $"Bearer {_authenticationContext.CurrentTokenString}");

    return await next(context);
}
```

authorizationにBearer～の形式でトークンを設定するのは、OAuthの仕組みに則っています。トークンはHTTPヘッダーに格納されて、メッセージとともにリモートへ送信します。

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

トークンの複合に失敗した場合は、認証エラー（401エラー）を返します。

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

ところで、このコードは動きません。IUserRepositoryでDapperを利用するのにTypeHandlerを作成したようにValueオブジェクトのIMessagePackFormatterを作成する必要があります。

Vendorオブジェクトのコードを見てみましょう。

```cs
public record Vendor(VendorId VendorId, 
    AccountNumber AccountNumber, 
    string Name, 
    CreditRating CreditRating, 
    bool IsPreferredVendor, 
    bool IsActive, 
    Uri? PurchasingWebServiceUrl, 
    TaxRate TaxRate, 
    ModifiedDateTime ModifiedDateTime, 
    IReadOnlyList<VendorProduct> VendorProducts);
```

多数のValueオブジェクトが含まれています。TaxRateを見てみると値がdecimalの構造体であることが見て取れます。

```cs
namespace AdventureWorks;

[UnitOf(typeof(decimal))]
public partial struct TaxRate
{
}
```

TaxRateは全ドメインで共通して利用するため、AdventureWorksプロジェクトに含めます。

このような値をMagicOnionで送受信するためには、TaxRate用のIMessagePackFormatterを作成する必要があります。Dapperのときと同じように、UnitGeneratorでは属性指定することで生成ができます。

```cs
[UnitOf(typeof(decimal), UnitGenerateOptions.MessagePackFormatter)]
public partial struct TaxRate
{
}
```

ただ同様にこうしてしまうと、ドメインのコードがMagicOnion（正確にはそのシリアライザーであるMessagePack）に依存してしまい、ドメインのフレームワーク非依存が破壊されてしまいます。

Dapperのときと同様に、アーキテクチャ的に受け入れるという選択肢もあります。ただ、おなじくあまり好みではないため、プロジェクトは分けることにします。

AdventureWorks.TaxRateのMagicOnion用のIMessagePackFormatterなので、プロジェクトとしてはAdventureWorks.MagicOnionに含めましょう。

```cs
namespace AdventureWorks.MagicOnion;

public class TaxRateFormatter : IMessagePackFormatter<TaxRate>
{
    public void Serialize(ref MessagePackWriter writer, TaxRate value, MessagePackSerializerOptions options)
    {
        options.Resolver.GetFormatterWithVerify<System.Decimal>().Serialize(ref writer, value.AsPrimitive(), options);
    }

    public TaxRate Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new TaxRate(options.Resolver.GetFormatterWithVerify<System.Decimal>().Deserialize(ref reader, options));
    }
}
```

こんな感じのコードになります。やはり個別に実装するのは手間ですし、不具合も怖いのでT4なりなにかで自動生成するのがオススメです。

では忘れないうちに、TaxRateFormatterを実装ビューに反映しましょう。

![](Article03/スライド15.PNG)

VendorRepositoryの実装は、認証設計の際に説明したものと変わらない為、割愛します。

これでひととおり見たのですが、ひとつ気になるところがありました。IVendorRepositoryのクライアント側実装であるVendorRepositoryClientです。

```cs
private IAuthenticationContext _authenticationContext;
private Endpoint _endpoint;

public async Task<Vendor> GetVendorByIdAsync(VendorId vendorId)
{
    var server = MagicOnionClient.Create<IVendorRepositoryService>(
        GrpcChannel.ForAddress(_endpoint.Uri),
        new IClientFilter[]
        {
            new AuthenticationFilter(_authenticationContext)
        });
    return await server.GetVendorByIdAsync(vendorId);
}
```

問題はこの、MagicOnionClientからリモートのgRPCサーバーを呼び出す、gRPCクライアントの生成コードで毎回これを実装するには問題があります。

1. 適用するフィルターが変わったときに、クライアント呼び出しコードをすべて修正しないといけない
2. エンドポイントも個別に指定したくない
3. 単純にコードが多い

とうわけで、ファクトリーを作成して、これらを隠蔽しましょう。

```cs
public interface IMagicOnionClientFactory
{
    T Create<T>() where T : IService<T>;
}

public class MagicOnionClientFactory : IMagicOnionClientFactory
{
    private readonly IAuthenticationContext _authenticationContext;
    private readonly Endpoint _endpoint;

    public MagicOnionClientFactory(
        IAuthenticationContext authenticationContext,
        Endpoint endpoint)
    {
        _authenticationContext = authenticationContext;
        _endpoint = endpoint;
    }

    public T Create<T>() where T : IService<T>
    {
        return MagicOnionClient.Create<T>(
            GrpcChannel.ForAddress(_endpoint.Uri),
            new IClientFilter[]
            {
                new AuthenticationFilter(_authenticationContext)
            });
    }
}
```

こんな感じでファクトリー側にコードを押し出して、利用する場所ではつぎのように使います。

```cs
private readonly IMagicOnionClientFactory _clientFactory;
public async Task<Vendor> GetVendorByIdAsync(VendorId vendorId)
{
    var server = _clientFactory.Create<IVendorRepositoryService>();
    return await server.GetVendorByIdAsync(vendorId);
}
```

ではこれを実装ビューに反映しましょう。

![](Article03/スライド16.PNG)

AdventureWorks全体で利用するMagicOnion用の、クライアント限定オブジェクトなので、AdventureWorks.MagicOnion.Clientプロジェクトに置くことにしました。

これは認証やデータベースと同列のものなので、ドメインモデルにも反映する必要がるでしょう。ということで、反映したのが下図になります。

![](Article03/スライド17.PNG)

認証ドメイン視点がこちら。そして

![](Article03/スライド18.PNG)

購買ドメイン視点だとこちらになります。さて、気が付いた方はいらっしゃるでしょうか？普通に読んでいるとほとんどの方は気が付かないと思うのですが、ここにきて重大な設計ミスが発覚しました。

![](Article03/スライド19.PNG)

ここの2つのネーミングがプロジェクトの命名規則を違反しています。

- AdventureWorks.MagicOnion
- AdventureWorks.MagicOnion.Client

プロジェクトの親子関係は、子は親を具体化したもので、親は子の抽象となるように設計すると初期で宣言しました。

ところが、AdventureWorks.MagicOnionはAdventureWorksのMagicOnion実装で、AdventureWorksドメイン固有のものです。

それに対してAdventureWorks.MagicOnion.Clientは全ドメインから汎用ドメインとして利用される、MagicOnionのクライアントサポートライブラリです。

名前空間的に子であるAdventureWorks.MagicOnion.Clientのほうが、概念的にはスコープが広い状態になってしまっていて、ここだけ名前空間の設計が破綻しています。

ではどうするか？

結局、ビジネス的なツリー構造と、技術的なツリー構造が混ぜてしまったことが原因でしょう。ということで、そこを分離する必要があります。

とうことで、ビジネス関連のドメインをAdventureWorks.Businessとして、ビジネスの実現をサポートする汎用ドメインは、「AdventureWorks. 汎用ドメイン名」という形に修正しましょう。

今回の最大のインパクトある設計ミスです。AdventureWorksドメインもそうですが、購買・販売・製造ドメインの名前空間をすべて変更しなくてはなりません。実際のところ、ゼロからアーキテクチャ設計していると、こういったどんでん返しはそれなりに発生します。

ではまず、境界付けられたコンテキストを修正します。

![](Article03/スライド20.PNG)

つぎのようなツリー構造になります。

- AdventureWorks
   - ビジネス
      - 購買
      - 販売
      - 製造
   - データベース
   - 認証
   - MagicOnion

ビジネスを挟んだことで、逆にすっきりしたように感じます。

ここまで来れば明らかなんですが、ビジネスドメインのルートをAdventureWorksにしてしまうと、非ビジネスのドメインと命名がコンフリクトする可能性は排除できません。そのためトップをプロダクト名前空間にして、その下はビジネスのルートと、非ビジネスを並べる形が無難です。

コンテキストマップは変わらないので、つぎは実装ビューを見てみましょう。

![](Article03/スライド21.PNG)

ベージュ色の部分がビジネス関連のプロジェクトです。

左下のAdventureWorks.Businessの領域をのぞくと、だいたい上半分に認証系のコンポーネント、下半分に検証系のコンポーネントが配置されています。どうように右半分にクライアントサイド、左半分にサーバーサイドのコンポーネントが配置されています。
## 認証ドメインの配置ビュー

先ほど、認証ドメインの実装ビューを作成しました。つづいて、そこで設計されたコンポーネントを論理ノードに配置しましょう。

![](Article03/スライド22.PNG)

これだけだと分かりにくいので、認証と検証にわけて流れを追いつつ見てみましょう。

### 認証処理の確認

認証時の大まかな流れはつぎの通り。

![](Article03/スライド23.PNG)

アプリケーションの起動時にViewModelから、AdventureWorks.Authenticationコンポーネント（名前空間をすべて書くと長すぎるので、以後省略しつつ記載します。）のIAuthenticationServiceを経由して認証処理を呼び出します。IAuthenticationServiceの実装クラスであるAuthenticationServiceの含まれる～.Jwt.Rest.Clientコンポーネントをとおしてサーバーサイドを呼び出します。

サーバーサイドのリクエストは～.Jwt.Rest.ServerコンポーネントのAuthenticationControllerにRESTのリクエストが呼び出されます。AuthenticationControllerはWindows認証を利用してリモートのアカウントを取得し、AdventureWorks.BusinessコンポーネントのIUserRepositoryを利用して、データベースからUserを取得して認証処理を行います。このとき実体は～.Business.SqlServerコンポーネントのUserRepositoryが利用されます。

正しくUserが取得できたら、～.JwtコンポーネントのUserSerializerを利用してJWTトークンを作成して返却します。

～.Hosting.RestはサーバーサイドのWeb APIをホスティングするための全体を統括するコンポーネントです。

おおむねこんな流れですが、問題はなさそうです。

### 検証処理の確認

つづいて検証時の流れです。

![](Article03/スライド24.PNG)

ユーザー操作に応じてViewModelから～.PurchasingコンポーネントのIVendorRepositoryをとおしてVenderオブジェクトを取得します。IVendorRepositoryの実体はリモートのWeb API側にあります。そのため、IVendorRepositoryのgRPCクライアントである～Purchasing.MagicOnionコンポーネントのVendorRepositoryClientが呼び出されます。

その際、認証情報であるJWTを～.Authentication.MagicOnion.ClientコンポーネントのAuthenticationFilterで付与してからサーバーサイドが呼び出されます。

サーバーサイドでは～.Authentication.MagicOnion.ServerコンポーネントのAuthenticationFilterAttributeで、HTTPヘッダーからJWTを取得して検証します。

問題なければ～.Purchasing.MagicOnion.ServerコンポーネントのVendorRepositoryServiceが呼び出されます。VendorRepositoryServiceは～.Purchasing.SqlServerコンポーネントのVendorRepositoryクラスを利用してVendorオブジェクトを取得してクライアント側に返却します。

このときVendorオブジェクトに含まれるTaxRateのようなValueオブジェクトのgRPC上のシリアライズ・デシリアライズに～.Business.MagicOnionのFormatterが利用されます。

検証側もどうやら問題なさそうです。

さて、認証ドメインについてはいったんこのあたりにしましょう。まだ何かある可能性はありますが、認証ドメインだけみていても、コスパは悪そうです。他の要件を設計しながら、認証ドメインの設計に問題がないか検証していきましょう。
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
# ロギングアーキテクチャ

ここからはロギングアーキテクチャについて設計していきましょう。

まずはロギングの要件を明確にします。

## ロギング要件

今回は仮に、つぎのような要件があるものとします。

1. ログは可能な限り一貫して保管するため、基本的にはサーバーサイドに保管すること
2. ただしロギングサーバーが停止している場合も考慮し、エラーレベルのログはローカルファイルシステムにもあわせて保管すること
3. ログは将来的な分析に利用するため、構造化ログを利用すること
4. ログの出力には、ログ出力を要求した端末・ユーザーの情報を含めること
5. ログの出力レベルは、モジュールの再配布なく、サーバーサイドから変更可能であること

WPFアプリケーションの場合、要件5が実現できると、運用時に楽になることがあります。WPFアプリケーションの場合、Webアプリケーションとちがって再配布が容易ではないため、障害などの解析が必要になった際に、ログの出力レベルを再配布なく変更できことは、おおきなメリットとなります。

なお要件4に一部のログ属性について言及していますが、今回はこれ以外の属性については言及しません。とくにユーザー情報に関しては、認証処理と密接にかかわってくるため入れましたが、それ以外は実際のプロジェクトにおいて必要に応じて検討してください。

## ログ出力レベル設計

さて詳細な設計に入り前に、ログの出力レベルについて決定しておきましょう。ここからは仮の実装を行いながら設計します。出力レベルの認識がずれていると、あとから直すのは負担が大きいためです。

|レベル|通常時出力|クライアントサイド|サーバーサイド|
|--|:-:|--|--|
|Trace|×|Debugレベル以外のViewModelのメソッド呼び出し時|なし|
|Debug|×|ViewModelの画面遷移イベント発生時、またはCommand呼び出し時|なし|
|Information|〇|なし|HTTPのリクエスト、レスポンス時。認証後のAPI呼び出し前。|
|Warning|〇|アプリケーションの機能に影響を与える可能性のあると判断したとき。個別に相談の上決定し、つど本表を更新する。|同左＋認証エラー時|
|Error|〇|意図しない例外の発生時|同左|
|Critical|〇|システムの運用に致命的な問題が発生した場合|同左|

通常はInformationレベル以上は出力し、Debug以下は、障害の調査時などに必要に応じて一時的に利用します。

上記仕様の場合、サーバーサイドで例外が発生した場合、クライアントサイドでもWeb API呼び出しが例外となります。そのため、同一の例外が2重で出力されますが、取りこぼすよりは良いため上記定義とします。

個人的にはWarningの扱いが難しいといつも感じます。単純明快なルールを定められたことがありません。良い提案があればぜひ伺いたいです。

### WPF上のTrace、Debugログ

前述のとおり、基本的にViewModelのメソッドの呼び出し箇所でログを仕込みます。ただ、これを抜け漏れ誤りなく実装するのは大変です。

そこで今回はPostSharpというAOPライブラリを利用して、一括でログ出力コードを「編み込み」ます。AOPとはAspect Oriented Programming、アスペクト志向プログラミングのことで、横断的な関心毎を「アスペクト」として、その関心を必要とする場所に「編み込む」方法論のことです。

詳細な説明はここでは割愛しますが、ここではログ出力処理をアスペクトとしてViewModelのメソッドに埋め込みます。

AOPライブラリは各種ありますが、おおきく2種類のタイプに分けられます。

1. プロキシーオブジェクトを作ってメソッド呼び出しをインターセプトするタイプ
2. コンパイル時にILを操作するなどして、特定のコードを任意の場所に埋め込むタイプ

PostSharpは2のタイプのライブラリです。

- [https://www.postsharp.net/](https://www.postsharp.net/)

10種類までのアスペクトであれば無償版でも利用でき、実績も多いため今回はPostSharpを利用します。

具体的には、つぎのようなOnMethodBoundaryAspectを継承したアスペクトを作成します。

```cs
public class LoggingAspect : OnMethodBoundaryAspect
{
    public static ILogger Logger・・・

    public override void OnEntry(MethodExecutionArgs args)
    {
        var logLevel = GetLogLevel(args);
        Logger.Log(logLevel, "{Type}.{Method}({Args}) Entry", args.Method.ReflectedType!.FullName, args.Method.Name, args);
```

そしてViewModelプロジェクトのAssemblyInfo.csなどに、つぎのように記載します。

```cs
using AdventureWorks.Wpf.ViewModel;

[assembly: LoggingAspect(AttributeTargetTypes = "*ViewModel")]
```

これでビルド時にViewModelのメソッドの入り口でLoggingAspectクラスのOnEntryが呼び出されるコードが編み込まれます。

非常に簡単ですし、抜け漏れ誤りなく実装が可能になります。

## ロギングアーキテクチャ概要

前述の要件を満たすために、ざっくりつぎのようなアーキテクチャを検討します。

1. ロギングのインターフェイスには、Microsoft.Extensions.Loggingを利用する
2. ロギングの実装には、Serilogを利用する（要件3）
3. ログの出力はSQL Server上に保管する（要件1）
4. WPFのログ出力は、SerilogのSink（出力先を拡張する仕組み）を利用してgRPCでサーバーへ送信して保管する（要件1）
5. またあわせてエラーレベルのログは、FileSinkを利用してローカルにも出よくする（要件2）
6. WPF・Web APIともに、起動時にログ設定ファイルをサーバーより取得して適用する（要件5）
7. ログ出力時は認証アーキテクチャで検討した仕組みを適用してユーザーを特定してログ出力する（要件4）

上記を実現するため、ロギングWeb APIをgRPCで構築します。ざっくりした構成はつぎのとおりです。

![](Article03/スライド26.PNG)

ロギングAPIも認証は掛けたいため、認証APIを利用します。その上で、つぎのような相互作用をとります。

![](Article03/スライド27.PNG)

まず認証APIを呼び出して認証します。このときビジネスドメインのWeb APIとは別のaudienceを利用します。そのためWPFの起動時にビジネスドメインとロギングで最低2回の認証を呼び出します。

認証に成功したらロギング設定をロギングAPIから取得します。取得時にはビジネスドメインのWeb API同様にJWTで認証します。

あとは取得した設定情報に基づいてログを出力します。

ビジネスドメインのWeb APIノードは、その機能の提供時に直接データベースサーバーを利用します。そのためログ出力も直接データベースへ出力します。

ロギング時にログAPIを利用してもよいのですが、今回はノードを分散した耐障害性まで求めないものとして、直接データベースに出力することとします。

## ロギングのドメインビュー

さて、そろそろ慣れてきてもう気が付いていると思いますが、ロギングも汎用のライブラリ的に扱いますから、新しい汎用ドメインになります。

下図がロギングドメインからみた境界付けられたコンテキストです。

![](Article03/スライド28.PNG)

そして下図がコンテキストマップです。

![](Article03/スライド29.PNG)

ロギングコンテキストは、購買・製造・販売のコンテキストからカスタマー・サプライヤー関係で利用されます。

その際に、認証されたユーザーにのみ利用を許可するため、認証コンテキストとAdventureWorks.Businessコンテキストを利用します。

ロギングコンテキストは、WPFクライアントに対してgRPCでロギングAPIを提供するためMagicOnionコンテキストを利用します。

そしてログの出力はSqlServerに行うため、データベースコンテキストも利用します。

## ロギングの論理ビュー

ではロギングの論理ビューを設計しましょう。

![](Article03/スライド30.PNG)

だいたいこんな感じでしょうか？

今回中央からAdventureWorks.Businessドメインを省略しています。実際には認証処理のためにUserオブジェクトを参照するのですが、本質的には影響が小さい事と、ロギングドメインの外側にSerilogに直接依存している層を書きたかったので、省略しました。

最外周にMagicOnionが2カ所でてきます。これは別のサービスというわけではなく、片側にそろえると込み合ってしまうので、見やすくするために分けてあるだけです。

上側がログ出力側、下側が初期化処理側のオブジェクトが集まっています。

### 初期化処理

まずは初期化処理についてみてみましょう。

![](Article03/スライド31.PNG)

認証と同様に、アプリケーション起動時に初期画面のViewModelから初期化を行います。

ILoggingInitializerインターフェイスを呼び出すことで初期化を行います。実際にはLoggingInitializerクラスを注入して、クラスを呼び出します。

LoggingInitializerは、ISerilogConfigRepositoryインターフェイスを利用して設定をサーバーから取得して、Serilogを初期化します。

ISerilogConfigRepositoryインターフェイスの実体はSerilogConfigRepositoryClientクラスで、IMagicOnionFactoryを利用して、ISerilogConfigServiceインターフェイスの実体を取得して呼び出すことで、gRPCでサーバーサイドを呼び出します。

サーバーサイドではSerilogConfigServiceクラスが呼び出され、ISerilogRepositoryを使って、データベースから設定値を取得します。

ISerilogRepositoryインターフェイスの実体はSerilogRepositoryクラスで、このクラスからデータベースを呼び出して実際の設定値を取得します。

実装ビューの設計時に、実際にコードを追いつつ詳細に見たいと思います。

### ログ出力処理

ログ出力処理の流れはつぎの通りです。

![](Article03/スライド32.PNG)

ユーザーがViewで何らかの操作をすると、ViewModelが呼び出されます。

ViewModelのメソッドの入り口で、PoshSharpによって織り込まれたLoggingAspectが呼び出されて、ログを出力します。

ログ出力を指示されたSerilogは（ここは図には記載していません）、MagicOnionSinkを利用してMagicOnion経由でログを出力します。

MagicOnionSinkはIMagicOnionFactoryからILoggingServiceのインスタンスを取得して、サーバーサイドにログを渡します。

サーバーサイドではLoggingServiceがログを受け取り、ILogRepositoryでログを書き込みます。

ILogRepositoryインターフェイスの実体はLogRepositoryクラスで、ここで実際にデータベースにログが出力されます。

## ロギング実装ビューの設計

では先ほどの登場オブジェクトを、コンポーネントに割り振ってみましょう。

![](Article03/スライド33.PNG)

ひとまず問題なさそうです。

コードと流れをおって見ていきましょう。

### 初期化処理

つぎの流れで順番にみていきます。

![](Article03/スライド34.PNG)

アプリケーションの起動時、初期画面で認証を実施していた箇所に、ログ出力の初期化処理を追加します。

```cs
public class MainViewModel : INavigatedAsyncAware
{
    private readonly ILoggingInitializer _loggingInitializer;

    ・・・

    public async Task OnNavigatedAsync(PostForwardEventArgs args)
    {
        var authenticationResult = await _authenticationService.TryAuthenticateAsync();
        if (authenticationResult.IsAuthenticated
            && await _loggingInitializer.TryInitializeAsync())
        {
            await _presentationService.NavigateToMenuAsync();
        }
        else
        {
            _presentationService.ShowMessage(
                Purchasing.ViewModel.Properties.Resources.AuthenticationFailed,
                Purchasing.ViewModel.Properties.Resources.AuthenticationFailedCaption,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // アプリケーションを終了する。
            Environment.Exit(1);
        }
    }
}
```

DIされたILoggingInitializerを呼び出してロガーを初期化します。初期化に失敗した場合は、ユーザー認証エラーでアプリケーションを終了します。

ILoggingInitializerの実装クラス、LoggingInitializerはつぎの通りです。

```cs
public class LoggingInitializer : ILoggingInitializer
{

    private readonly ApplicationName _applicationName;
    private readonly ILoggerFactory _loggerFactory;

    ・・・

    public async Task<bool> TryInitializeAsync()
    {
        // ロギングドメインの認証処理を行う
        AuthenticationService authenticationService = 
            new(new ClientAuthenticationContext(), LoggingAudience.Instance);
        var result = await authenticationService.TryAuthenticateAsync();
        if (result.IsAuthenticated is false)
        {
            return false;
        }

        // ロギング設定を取得する
        MagicOnionClientFactory factory = new(result.Context, Endpoint);
        var repository = new SerilogConfigRepositoryClient(factory);
        var config = await repository.GetClientSerilogConfigAsync(_applicationName);
#if DEBUG
        config = config with { MinimumLevel = LogEventLevel.Debug };
#endif

        // ロガーをビルドする
        var logger = config.Build();

        // ロギング設定を適用する
        MagicOnionSink.MagicOnionClientFactory = factory;
        LoggingAspect.Logger = 
            new SerilogLoggerProvider(logger)
                .CreateLogger(typeof(LoggingAspect).FullName!);

        return true;
    }
}
```

ApplicationNameとILoggerFactoryをDIして初期化を行います。ログを出力する際、購買・製造・販売のどのアプリケーションのログが出力したか判別したいため、ApplicationNameを利用します。

初期化処理ではまずAuthenticationServiceを利用して、ロギングドメインの認証を行います。

認証が成功したら、SerilogConfigRepositoryClientを利用してリモートから設定情報を取得します。このとき、アプリケーション名を指定して取得します。アプリケーション名を指定することで、個別のアプリケーションごとに設定が変更できるようにします。

設定情報を取得したらロガーをビルドして、MagicOnionSinkとLoggingAspectを初期化します。このとき、開発中はログを詳細にだしておきたいのでデバッグビルド時は、ログレベルを強制的にLogEventLevel.Debugに上書きしています。

LoggingAspectはSerilogには直接依存せず、Microsoft.Extensions.Hosting.Loggingのロガーを利用するため、SerilogLoggerProviderからロガーを生成しています。

全体としてはこのような流れになります。では詳細をもう少し掘り下げて見ていきましょう。

```cs
public class SerilogConfigRepositoryClient : ISerilogConfigRepository
{
    private readonly IMagicOnionClientFactory _clientFactory;

    public async Task<SerilogConfig> GetClientSerilogConfigAsync(ApplicationName applicationName)
    {
        var service = _clientFactory.Create<ISerilogConfigService>();
        return await service.GetServerSerilogConfigAsync(applicationName.Value);
```

SerilogConfigRepositoryClientはこれまでのMagicOnionのクライアント実装と相違ありません。IMagicOnionClientFactoryからサービスを生成して、リモートを呼び出します。

呼び出されたリモート側はつぎの通りです。

```cs
public class SerilogConfigService : ServiceBase<ISerilogConfigService>, ISerilogConfigService
{
    private readonly ISerilogConfigRepository _repository;

    public async UnaryResult<SerilogConfig> GetServerSerilogConfigAsync(string applicationName)
    {
        return await _repository.GetClientSerilogConfigAsync(new ApplicationName(applicationName));
    }
}
```

ISerilogConfigRepositoryを呼び出してその結果を返却しています。実体はSQL Server向けのSerilogConfigRepositoryクラスです。

```cs
public class SerilogConfigRepository : ISerilogConfigRepository
{
    ・・・
    public async Task<SerilogConfig> GetClientSerilogConfigAsync(ApplicationName applicationName)
    {
        return await GetSerilogConfigAsync(applicationName, new ApplicationName("Client Default"));
    }

    private async Task<SerilogConfig> GetSerilogConfigAsync(
        ApplicationName applicationName, 
        ApplicationName defaultName)
    {
        using var connection = _database.Open();

        const string query = @"
select
	ApplicationName, 
	MinimumLevel,
	Settings
from
	Serilog.vLogSettings
where
	ApplicationName = @Value";

        return await connection.QuerySingleOrDefaultAsync<SerilogConfig>(query, applicationName) 
               ?? await connection.QuerySingleAsync<SerilogConfig>(query, defaultName);
    }
}
```

ログの設定はアプリケーションごとに定義できると記載しました。ただ、つねに個別の設定がしたいわけではなく、通常時はデフォルトの設定を利用しておいて、障害時などに個別に設定が変更できるようにしたいです。

そのため、GetClientSerilogConfigAsyncを呼び出すと"Client Default"をデフォルト設定名として内部でGetSerilogConfigAsyncを呼び出しています。

まずは指定されたアプリケーション名で取得してみて、個別の定義がなければ"Client Default"の設定を取得して返却します。

SQLの中でSettings列が指定されていますが、ここに通常は設定ファイルに記載するフォーマットで設定を格納しておきます。これによってデータベース側の設定を更新することで、すべてのWPFアプリケーションに適用されるログ設定を変更可能にします。

取得した設定は、クライアント側でビルドされてロガーインスタンスを生成します。

```cs
public record SerilogConfig(ApplicationName ApplicationName, LogEventLevel MinimumLevel, string Settings)
{
    public ILogger Build()
    {
        var settingString = Settings
            .Replace("%MinimumLevel%", MinimumLevel.ToString())
            .Replace("%ApplicationName%", ApplicationName.Value);

        using var settings = new MemoryStream(Encoding.UTF8.GetBytes(settingString));
        var configurationRoot = new ConfigurationBuilder()
            .AddJsonStream(settings)
            .Build();

        global::Serilog.Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configurationRoot)
#if DEBUG
            .WriteTo.Debug()
#endif
            .CreateLogger();

        return global::Serilog.Log.Logger;
    }
}
```

Settingsにはログレベルやアプリケーション名が変更できるように、置換文字列で記載しておいて、ビルド時に値を置き換えます。

その上で、置き換えられた設定からConfigurationBuilderを利用して設定オブジェクトをビルドします。そしてビルドされた設定からSerilogのロガーを生成します。

このとき、やはりデバッグ時はVisual StudioのDebugコンソールにも出したいので、WriteTo.Debug()を追加しています。

初期化処理はおおむねこの通りです。

### ログ出力処理

ではログ出力側のコードを見ながら流れを確認していきましょう。

先に説明した通り、ログの出力はViewModelのメソッド入り口で行われます。ただ個別に実装するわけではなく、LoggingAspectを織り込むことで行われます。

```cs
[PSerializable]
public class LoggingAspect : OnMethodBoundaryAspect
{
    public static ILogger Logger { get; set; } = new NullLogger<LoggingAspect>();

    public override void OnEntry(MethodExecutionArgs args)
    {
        var logLevel = GetLogLevel(args);
        Logger.Log(logLevel, "{Type}.{Method}({Args}) Entry", args.Method.ReflectedType!.FullName, args.Method.Name, args);
    }

    private static LogLevel GetLogLevel(MethodExecutionArgs args)
    {
        return args.Method.Name.StartsWith("On")
               || args.Method.GetCustomAttributes(true).Any(a => a is RelayCommandAttribute)
            ? LogLevel.Debug
            : LogLevel.Trace;
    }
}
```

OnMethodBoundaryAspectのOnEntryをオーバーライドすることで、メソッドの呼び出しをフックします。

呼び出されたメソッドをGetLogLevelに渡して、ログの出力レベルを判定します。画面遷移時とコマンドの呼び出し時はDebug、それ以外はTraceで出力する仕様なので、そのように判定します。

Kamishibaiを利用する場合、画面遷移イベントはOn～というメソッドを利用するためそれで判断します。画面遷移イベント以外にOn～を使うことは避けますが、Debugレベルのログは通常時は出力しない為、少々別のものが紛れ込んでも問題ないでしょう。

ViewModelのコマンドは、CommunityToolkit.Mvvmライブラリを利用して行います。具体的にはつぎのように記載します。

```cs
[RelayCommand]
private Task GoBackAsync() => _presentationService.GoBackAsync();
```

メソッドにRelayCommandAttributeを宣言することで、コード生成を利用して自動的にコマンドが作成されます。非常に便利なのでオススメです。

このようにコマンドを自動生成するため、メソッドにRelayCommandAttributeが宣言されているかどうかで、コマンドの呼び出しかどうかを判定しています。

LoggingAspectからILoggerを呼び出すと、内部的にMagicOnionSinkが呼び出されます。

```cs
public class MagicOnionSink : ILogEventSink
{
    private readonly LogEventLevel _restrictedToMinimumLevel;

    public async void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < _restrictedToMinimumLevel)
        {
            return;
        }

        var service = MagicOnionClientFactory.Create<ILoggingService>();
        await service.RegisterAsync(
            new LogDto(
                ・・・
            ));
    }
}
```

先頭で呼び出されたログの出力レベルが、出力条件を満たしているか判定し、見たいしていない場合は早期リターンします。

条件を満たしたレベルであれば、MagicOnionClientFactoryを利用してILoggingServiceを生成し、リモートにログを送信します。

ここは細かな文字列編集などが多いため、一部のコードを省略していますので、詳細に興味があるかたはGitHubを直接ご覧ください。

ログを送信されたサーバー側はつぎのとおりです。

```cs
public class LoggingService : ServiceBase<ILoggingService>, ILoggingService
{
    private readonly ILogRepository _eventRepository;
    private readonly IAuthenticationContext _authenticationContext;

    public LoggingService(
        ILogRepository eventRepository, 
        IAuthenticationContext authenticationContext)
    {
        _eventRepository = eventRepository;
        _authenticationContext = authenticationContext;
    }

    public async UnaryResult RegisterAsync(LogDto logRecord)
    {
        await _eventRepository.RegisterAsync(
            new Log(
                ・・・
            ));
    }
}
```

JWTによる認証情報と、ILogRepositoryを利用してSQL Serverにログを保管します。ILogRepositoryの実装は繰り返しになるため、省略します。

こうしてログの出力設定をデータベース側に保管しておいて、任意のタイミングで出力レベルを調整できるようにしています。

その上でWPFアプリケーションの個別機能の実装側で特別な実装を行うことなく、抜け漏れ誤りなくログが出力できるようになりました。
## ロギング配置ビューの設計

それでは実装ビューで導出したコンポーネントをノード上に配置しましょう。とくに難しい要素もなく、これまでの繰り返しになってしまうため、さらっと説明します。

実装ビューのコンポーネントをノード上に配置したものが以下の図です。

![](Article03/スライド36.PNG)

初期化処理時のシーケンスをたどってみましょう。

![](Article03/スライド37.PNG)

問題ありませんね。ロギング処理時のパスも、ViewModelからのほぼ同様なので説明は割愛します。

1点補足があるとすると、購買API側のログ出力は、SerilogのSqlServerSinkを直接使うことです。そのため、特別な仕組みは必要ありません。







# 代表的なユースケースの実現

長くなりましたが、やっとユースケースを実装するための土台ができました。

ここからは個別のユースケースの実現を設計することで、WPFアプリケーション全体のアーキテクチャを完成させていきます。

あらためてユースケースビューを見てみましょう。

|No.|ユースケース|アーキテクチャパターン|代表|選定理由|
|--|--|--|--|--|
|1|ログインする|ログインパターン|✅|アプリケーション起動時の処理を実現するため。厳密にはログインはユースケースとは言えないかも知れないが、機能をユースケースで網羅するため含める。|
|2|発注する|基本パターン|||
|3|再発注する|基本パターン|✅|ユースケース固有の複雑な参照処理とそれを表示する高機能なグリッド、エンティティのCRUDが含まれるため。実際にはUとDは含まれないが、アーキテクチャ的にはCと差異がない。|
|4|・・・|・・・|||

今回は「ログインする」と「再発注する」の2つのユースケースを実現する過程で、アーキテクチャを煮詰めていきます。

つぎのような流れでユースケースを実現ます。

1. ユースケースの実現に必要なシナリオの一覧を洗い出す
2. 洗い出したシナリオを記述する
3. シナリオの記述を実現しながら、各ビューを完成させていく

## 「ログインする」ユースケースの実現

### シナリオ一覧

まずは「ログインする」を実現する上で、必要なシナリオを洗い出します。

|No.|シナリオ|説明|
|--|--|--|
|1|正常にログインする||
|2|認証に失敗する||

アーキテクチャを設計する上で必要になりそうなシナリオはこの2つでしょう。説明欄は、シナリオ名で十分な場合は省略しても構いません。むしろシナリオ名に含まれる内容を重複して含めた場合、読み手にムダな時間を強いることになりますし、修正時のコストも高くなり、メンテナンス性が低下します。意味なく重複した記述は避けるのが私の好みです。

では個別のシナリオを記述していきましょう。

### シナリオ「正常にログインする」

シナリオでは、ユーザーとシステムとの対話を記述します。前提条件と、事後条件も明確にしておくことがポイントです。

【前提条件】

* システムは起動していないこと

|No.|ユーザーの振る舞い|システムの振る舞い|
|--|--|--|
|1|システムを起動する|トップ画面を表示する|
|2||認証処理を行う|
|3||認証に成功する|
|4||メニュー画面を表示する|

【事後条件】

* システムはメニュー画面を表示している事

さてここでは、仕様を定義しているわけではなく、あくまでアーキテクチャを設計しています。そのため、この時点で正確な仕様は必要なく、アーキテクチャを設計できるレベルで十分です。

また今後このユースケース仕様を詰めていくうちに変更されたとしても構いません。その際に、アーキテクチャに影響する変更がなければ、この記述を修正する必要はありません。

あくまでも、アーキテクチャを設計する上で必要十分な仕様を定義することが目的です。

ではシナリオの「システムの振る舞い」にしたがって、実装を振り返りつつ、アーキテクチャを蒸留していきましょう。

#### トップ画面を表示する

まずはエントリーポイントのProgram.csを見てみましょう。

```cs
var builder = KamishibaiApplication<App, MainWindow>.CreateBuilder();

// MagicOnionのクライアントファクトリーをDIコンテナーに登録する。
builder.Services.AddSingleton(GetServiceEndpoint());
builder.Services.AddSingleton<IMagicOnionClientFactory, MagicOnionClientFactory>();

// 認証サービスを初期化する。
builder.Services.AddSingleton(PurchasingAudience.Instance);
var authenticationContext = new ClientAuthenticationContext();
builder.Services.AddSingleton(authenticationContext);
builder.Services.AddSingleton<IAuthenticationContext>(authenticationContext);
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

// ロギングサービスの初期化。
var applicationName = new ApplicationName("AdventureWorks.Business.Purchasing.Hosting.Wpf");
builder.Host.UseMagicOnionLogging(applicationName);

// MagicOnionのIFormatterResolverを準備する
FormatterResolverBuilder resolverBuilder = new();

// AdventureWorks.BusinessドメインのMagicOnionで利用するIFormatterResolverを登録する
resolverBuilder.Add(AdventureWorks.Business.MagicOnion.CustomResolver.Instance);

// View & ViewModelを初期化する。
// メニュー
builder.Services.AddPresentation<MainWindow, MainViewModel>();
builder.Services.AddPresentation<MenuPage, MenuViewModel>();

// MagicOnionのIFormatterResolverを初期化する
resolverBuilder.Build();

// アプリケーションのビルド
var app = builder.Build();

// 未処理の例外処理をセットアップする。
app.Startup += SetupExceptionHandler;
await app.RunAsync();
```

一般的なGeneric Hostを起動するコードですね。ただ正直、好みのコードとは言えません。このコードにはいくつかの問題があります。

まず大前提としてWPFアプリケーションは、購買アプリケーションだけでなく、製造・販売ドメインにも存在します。それらのアプリケーションでも、つぎのコードは完全に同じ形で登場するでしょう。

1. MagicOnionのクライアントファクトリーのDIコンテナーへの登録
2. 認証サービスのDIコンテナーへの登録
3. AdventureWorks.BusinessドメインのMagicOnionで利用するIFormatterResolverの登録

DIコンテナーの初期化コードは、意図しないところでバグを生みがちです。

たとえば購買アプリケーションを構築している途中に、認証サービスの修正が必要なことが判明して、DIコンテナーに登録するオブジェクトが増えたとしましょう。その場合、上述のProgram.csのコードを修正する必要があります。この時、本来は製造・販売ドメイン側の修正も必要になりますが、そういった影響範囲を常に正しく判断して管理することは難しいです。

またWPF間だけではなく、WPFとMagicOnionサーバー側でも同様の問題が発生します。MagicOnionで通信するためには、CustomResolverをクライアント側とサーバー側で同期をとってメンテナンスする必要がありますが、そういったところも抜けがちです。

そのため、各コンポーネントでDIコンテナーの初期化に必要な処理は、前述のロギングサービスの初期化のように、コンポーネント側に初期化用の拡張メソッドを用意しておくのが良いでしょう。

たとえば認証サービスであればつぎのような拡張メソッドを用意してはどうでしょうか？

```cs
public static class ServiceCollectionExtensions
{
    public static void UseJwtRestClient(this IServiceCollection services, Audience audience)
    {
        services.AddSingleton(audience);
        var authenticationContext = new ClientAuthenticationContext();
        services.AddSingleton(authenticationContext);
        services.AddSingleton<IAuthenticationContext>(authenticationContext);
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
    }
}
```

するとProgram.csはつぎのようになります。

```cs
// 認証サービスを初期化する。

// Before
builder.Services.AddSingleton(PurchasingAudience.Instance);
var authenticationContext = new ClientAuthenticationContext();
builder.Services.AddSingleton(authenticationContext);
builder.Services.AddSingleton<IAuthenticationContext>(authenticationContext);
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

// After
builder.Services.UseJwtRestClient(PurchasingAudience.Instance);
```

このようにすることで、各コンポーネントの初期化処理をコンポーネント側に閉じ込めることができます。ではこれで良いか？というと実は不十分です。

認証処理の初期化コードのすぐ下につぎのようなコードがあります。

```cs
// ロギングサービスの初期化。
var applicationName = new ApplicationName(typeof(Program).Assembly.GetName().Name!);
builder.Host.UseMagicOnionLogging(applicationName);
```

認証処理ではbuilder.Serviceが使われていますが、ロギングサービスではbuilder.Hostが使われています。両方つかうコンポーネントがでてきてもおかしくありません。そのためIServiceCollectionの拡張メソッドでは初期化が実現できないケースが出てくる可能性があります。

となるとbuilderを渡したら良いのではないか？となりますが、builderはWPF側とASP.NET側で共通のインターフェイスが用意されていません。DIコンテナーの初期化は、各WPFアプリケーション間でも同期をとってメンテナンスする必要がありますが、WPFアプリケーションとASP.NETアプリケーション間でも同期をとってメンテナンスする必要があります。

DIコンテナーの初期化に誤りがあると、コンパイル時にはエラーにならなかったのに、起動してみたらDIコンテナーでエラーがでて、ぱっと見なにが悪いのか分からないなんてことは良くあります。このあたりがDIが難しいと感じさせている一因な気がします。

本来はbuilderが抽象化できて、WPFでもASP.NETでも同様に扱えると良いのですが、WebApplicationBuilderがプラットフォームに依存しないインターフェイスを持たない為、直接的に解決できません。

というわけで、WPFとASP.NETのbuilderを抽象化して共通で扱えるインターフェイスを作って、それぞれのアダプターを用意することで解決しましょう。

具体的には、プラットフォームに共通するつぎのようなインターフェイスを作成します。

```cs
public interface IApplicationBuilder
{
    IServiceCollection Services { get; }
    IConfiguration Configuration { get; }
    IHostBuilder Host { get; }
    FormatterResolverBuilder FormatterResolver { get; }
}
```

そしてWPF用の実装クラスを用意します。

```cs
public class WpfApplicationBuilder<TApplication, TWindow> : IApplicationBuilder
    where TApplication : Application
    where TWindow : Window

{
    private readonly IWpfApplicationBuilder<TApplication, TWindow> _applicationBuilder;

    public WpfApplicationBuilder(IWpfApplicationBuilder<TApplication, TWindow> applicationBuilder)
    {
        _applicationBuilder = applicationBuilder;
    }

    public IServiceCollection Services => _applicationBuilder.Services;
    public IConfiguration Configuration => _applicationBuilder.Configuration;
    public IHostBuilder Host => _applicationBuilder.Host;
    public FormatterResolverBuilder FormatterResolver { get; } = new();
}
```

ではあらためて、認証サービスの初期化拡張メソッドを用意しましょう。

```cs
public static class ApplicationBuilderExtensions
{
    public static void UseJwtRestClient(this IApplicationBuilder builder, Audience audience)
    {
        builder.Services.AddSingleton(audience);
        var authenticationContext = new ClientAuthenticationContext();
        builder.Services.AddSingleton(authenticationContext);
        builder.Services.AddSingleton<IAuthenticationContext>(authenticationContext);
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
    }
}
```

これを類似箇所にすべてあてはめた後のProgram.csはつぎのようになります。

```cs
var kamishibaiBuilder = KamishibaiApplication<App, MainWindow>.CreateBuilder();
var builder = new AdventureWorks.Hosting.Wpf.WpfApplicationBuilder<App, MainWindow>(kamishibaiBuilder);

// MagicOnionのクライアントファクトリーをDIコンテナに登録する。
builder.UseMagicOnionClient(GetServiceEndpoint());

// 認証サービスを初期化する。
builder.UseJwtRestClient(PurchasingAudience.Instance);

// ロギングサービスの初期化。
var applicationName = new ApplicationName("AdventureWorks.Business.Purchasing.Hosting.Wpf");
builder.Host.UseMagicOnionLogging(applicationName);

// AdventureWorks.BusinessドメインのMagicOnionで利用するIFormatterResolverを登録する
builder.UseBusinessMagicOnion();

// View & ViewModelを初期化する。
builder.UsePurchasingView();

// MagicOnionのIFormatterResolverを初期化する
builder.FormatterResolver.Build();

// アプリケーションのビルド
var app = kamishibaiBuilder.Build();

// 未処理の例外処理をセットアップする。
app.Startup += SetupExceptionHandler;
await app.RunAsync();
```

だいぶスッキリしました。

ただよく見ると、購買・製造・販売のWPFアプリケーションで共通化できそうな処理がまだ含まれています。

1. WpfApplicationBuilderの初期化
2. MagicOnionのクライアントファクトリーの初期化
3. 認証サービスの初期化
4. ロギングサービスの初期化
5. FormatterResolverの初期化
6. 未処理の例外処理のセットアップ

とうことでWpfApplicationBuilderを少し拡張して、これらの処理をまとめてしまいましょう。

```cs
public class WpfApplicationBuilder<TApplication, TWindow> : IApplicationBuilder
    where TApplication : Application
    where TWindow : Window

{
    ・・・
    public static WpfApplicationBuilder<TApplication, TWindow> CreateBuilder()
    {
        return new(KamishibaiApplication<TApplication, TWindow>.CreateBuilder());
    }

    public IHost Build(ApplicationName applicationName, Audience audience)
    {
        // MagicOnionのクライアントファクトリーをDIコンテナに登録する。
        this.UseMagicOnionClient(GetServiceEndpoint());

        // 認証サービスを初期化する。
        this.UseJwtRestClient(audience);

        // ロギングサービスの初期化。
        this.Host.UseMagicOnionLogging(applicationName);
        
        // MagicOnionの初期化
        FormatterResolver.Build();
        
        // アプリケーションのビルド
        var app = _applicationBuilder.Build();

        // 未処理の例外処理をセットアップする。
        app.Startup += SetupExceptionHandler;
        return app;
    }
}
```

WpfApplicationBuilderをインスタンス化するstaticメソッドと、Buildメソッドを追加しました。そしてBuildメソッドの中で、2～6の処理をまとめています。

これでProgram.csはつぎのようになりました。

```cs
var builder = AdventureWorks.Hosting.Wpf.WpfApplicationBuilder<App, MainWindow>.CreateBuilder();

// View & ViewModelを初期化する。
builder.UsePurchasingView();

// アプリケーションのビルド
var applicationName = new ApplicationName("AdventureWorks.Business.Purchasing.Hosting.Wpf");
var app = builder.Build(applicationName, PurchasingAudience.Instance);

await app.RunAsync();
```

WPFで横断的に共通な処理はWpfApplicationBuilderに含めることで、アプリケーション固有の部分だけが残り、よりシンプルになりましたし、メンテナンス性も向上しました。

さて、これらの処理もデータベースや認証と同様の粒度なので、境界付けられたコンテキストとコンテキストマップを更新しましょう。

![](Article03/スライド39.PNG)

![](Article03/スライド40.PNG)

ホスティングは、先ほどのIApplicationBuilderが含まれるため、ほぼすべてから依存されます。そのため関連は省略しています。

今回開発しているシステムは、クライアントサイドもサーバーサイドもすべてGeneric Hostが前提となっています。そのため、Microsoft.Extensions.Hostingに依存していますが、Generic Host上のロガーインターフェイスであるILogger<T>がそのライブラリから依存されています。そのためホスティングコンテキストについても、ほぼすべてから依存されていても問題ないと判断しました。

むしろWPFとサーバーサイドでコンポーネントを共通化するにあたり、コンポーネントの初期化処理含めて共通化するために、WPF側もGeneric Hostに載せている側面があります。そのためMicrosoft.Extensions.Hostingに依存することは問題ありません。

つづいて、ではIApplicationBuilderなどを含めるために実装ビューを更新しましょう。

![](Article03/スライド41.PNG)

コンポーネントをフルネームで記述すると混雑しすぎるし、文字が小さくなりすぎるので、親のパッケージ名を省略しています。たとえば左上のBusinessコンポーネントはAdventureWorks.Businessが正式名称ですが、親のAdventureWorksを省略しています。

AdventureWorksドメインがトップにあり、その下にBusiness・Wpf・Logging・Authentication・MagicOnionの5つのサブドメインがあります。Businessサブドメインの下にはさらにPurchasingサブドメインがあります。

Purchasingサブドメインが、現在アーキテクチャ設計している購買システムになります。それ以外は、製造・販売システムからも共有される領域になります。

なお外部コンポーネントへの依存は、たとえばPurchasing.ViewModelもPostSharpなどに依存しますが、全部書くと線で埋まってしまいます。そのため推移的に依存することにして一部省略しています。

Hostingドメインが右下に追加し、HostingコンポーネントとHosting.Wpfコンポーネントに、それぞれIApplicationBuilderとWpfApplicationBuilderを追加しました。

さて、起動処理について整理が済みましたので、「トップ画面を表示する」続きを見直していきましょう。

さきほど、Program.csの中で、つぎのようなコードが含まれていました。

```cs
var builder = AdventureWorks.Hosting.Wpf.WpfApplicationBuilder<App, MainWindow>.CreateBuilder();

・・・

var app = builder.Build(applicationName, PurchasingAudience.Instance);
await app.RunAsync();
```

RunAsyncを呼び出すことで、MainWindowが起動されます。

MainWindowは、DIコンテナー初期化時に次のように登録されています。

```cs
builder.Services.AddPresentation<MainWindow, MainViewModel>();
```

そのため、MainWindow起動時にMainViewModelがDataContextにバインドされます。MainViewModelはINavigatedAsyncAwareを実装しているため、MainWindowが表示されたタイミングでOnNavigatedAsyncメソッドが呼び出されます。

```cs
public class MainViewModel : INavigatedAsyncAware
{
    ・・・
    public async Task OnNavigatedAsync(PostForwardEventArgs args)
    {
        ・・・
```

#### 認証処理を行う & 認証に成功する & メニュー画面を表示する

OnNavigatedAsyncメソッドの中で、認証処理を行います。このときシステム的には、販売サービスのgRPCとロギングサービスのgRPCに対して、認証処理を行う必要があります。

認証が成功したら、メニュー画面を表示します。

```cs
public class MainViewModel : INavigatedAsyncAware
{
    ・・・
    public async Task OnNavigatedAsync(PostForwardEventArgs args)
    {
        var authenticationResult = await _authenticationService.TryAuthenticateAsync();
        if (authenticationResult.IsAuthenticated
            && await _loggingInitializer.TryInitializeAsync())
        {
            await _presentationService.NavigateToMenuAsync();
        }```
```

ここはとくにアーキテクチャ的な修正はありませんね。ということで、シナリオ「正常にログインする」のアーキテクチャ設計は完了です。

### シナリオ「認証に失敗する」

では、次にシナリオ「認証に失敗する」を見ていきましょう。

【前提条件】

* システムは起動していないこと

|No.|ユーザーの振る舞い|システムの振る舞い|
|--|--|--|
|1|システムを起動する|トップ画面を表示する|
|2||認証処理を行う|
|3||認証に失敗する|
|4||認証失敗をアラートで通知する|
|5|アラートダイアログを閉じる|システムを終了する|


【事後条件】

* システムは終了していること

2までは成功時のシナリオと同じですね。

シナリオの記述は、差分が小さいことはありがちです。そのため、差分だけ記述してもよいですし、全体が小さいならすべて書いてしまっても良いでしょう。そこは単一のルールを定めず、使い分けるのが良いと思います。

なお実装的には、アーキテクチャに影響するような内容は含まれない為、ここでは省略します。
## 「再発注する」ユースケースの実現

さて、やっとここまできました。ユースケースを実現するためのアーキテクチャを設計していきましょう。

### シナリオ一覧

再発注するユースケースのシナリオ一覧は下記のとおりです。

|No.|シナリオ|説明|
|--|--|--|
|1|再発注する||
|2|・・・||

今回はユースケースの正常系となるシナリオ「再発注する」を取り上げることで、アーキテクチャ設計の手順を説明します。

### シナリオ「再発注する」

シナリオは下記のとおりです。

【前提条件】

- システムはメニュー画面を表示していること

|No.|ユーザーの振る舞い|システムの振る舞い|
|--|--|--|
|1|メニューから再発注を選択する|要再発注製品一覧画面に遷移する|
|2||再発注の必要な製品を表示する|
|3|再発注するベンダーを選択する|再発注画面に遷移する|
|4||選択されたベンダーと再発注が必要な製品の一覧を表示する|
|5|発注ボタンを押下する|発注する|
|6||発注が成功したことを、メッセージダイアログで表示する|
|7||要再発注製品一覧画面に遷移する|
|8||再発注の必要な製品を表示する|

【事後条件】

- 発注の明細がデータベースに登録されている
- システムは要再発注製品一覧画面に遷移し、再発注が必要な製品を表示している

シナリオにはいくつかの画面が登場します。この時点で正確な画面設計は不要ですが、まったくないのも進めずらいです。そこで、ざっくりしたスケッチを書いておくと良いでしょう。

この時点ではワイヤーフレームで十分です。紙芝居的にコミュニケーションにも使いたいので、私はPowerPointをよく利用しています。デザイン的な素養がない私でも使いこなせるので最高です。

#### 「要再発注製品一覧」画面

![](Article03/スライド42.PNG)

再発注が必要な製品の一覧を表示する画面です。この画面から再発注するベンダーを選択します。ベンダー列とカテゴリー列は、同一の行は結合して表示し、ベンダー単位で中計金額を表示します。

業務的には非常によくありがちな画面です。よくありがちで、案外大変な画面です。

業務アプリケーションではグリッドの操作性は命とも言えます。そのため、私の辞書には「グリッドを自作する」とういう言葉はありません。グリッドは必ずサードパーティ製のものを利用します。

この画面ではGrapeCityさんのSPREAD for WPFに含まれるGcSpreadGridコントロールを利用します。

![](Article03/GcSpreadGrid.png)

GcSpreadGridは、高度なデータグリッド機能を提供するコンポーネントです。このグリッドは、Excelのような機能を持ち、セルの書式設定やフィルタリング、ソート、グループ化、集計、およびデータの編集など多様な操作を可能にします。大量のデータに対しても高速なパフォーマンスを提供するため、大規模なデータセットにも対応しています。またグリッドのデザイナーも提供されており、開発も容易です。

あまりに多機能なので、すべての仕様を文書化して仕様書を作ることは困難です。そのため、仕様上機能を大きく制限して、文書化できる範囲で利用しているケースがよく見られます。しかし、それは余りに勿体ないです。

そこで私はお客様とつぎのような合意の上で、大きな制限を設けずに利用することを提案しています。

「サードパーティコントロールで標準的に有効化されている機能は、システムの仕様書としては明記せず、その動作は、サードパーティの仕様に準拠するものとする。ユーザーテストにおいて十分に評価し、取り決められたテスト期間中に、受け入れ可能な範囲で詳細の変更は行う。リリース後、不具合をのぞき、サードパーティー製コントロールの挙動について瑕疵対応は行わない。」

一見責任放棄のようですが、顧客としては最高の操作性をもつグリッドを利用できるため、大抵の場合は受け入れて頂けます。そのために場合によって、事前にモックを活用してお客様に評価していただくこともあります。

受託開発の場合、この手の融通が利かないことで、開発側・ユーザー側双方の足かせになることがおおいです。しかしお客様と良い関係が築けていて、かつお客様が開発に理解を示してくれる場合は、このような合意を取り付けることを考えても良いのではないでしょうか？

#### 「再発注」画面

![](Article03/スライド43.PNG)

「要再発注製品一覧」画面で選択されたベンダーに対して再発注するための画面。配送方法を選択肢して再発注する。

### ユースケースとコンポーネント

さて、この時点で私が最初に考えるのは、ユースケースを実現する上でどうコンポーネントを分割するか？ということです。

ということで、現時点でWPFアプリケーションのコンポーネント構成を見てみましょう。Businessドメイン以外は省略します。

![](Article03/スライド44.PNG)

さてここで考えたいのは、ユースケースを実現するオブジェクトをどこに実装していくかです。

新たに実装するものはWPF上だけを考えるとつぎの5種類です。

1. View
2. ViewModel
3. 再発注ユースケースでのみ利用するオブジェクト
4. 3をサーバーから取得するクライアント実装
5. 4で利用するクライアントとサーバー間のインターフェイス定義

このうちViewだけはPurchasing.Viewの1つにまとめて、それ以外はユースケースごとに分割して実装することにします。

ユースケース間の依存関係は、丁寧に設計する必要があります。無作為に実装していると、ユースケース間で依存の循環が発生してしまいがちです。画面は双方向に移動するケースがあったり、ユースケース間で非常に似通ったオブジェクトが登場するからです。しかしここの依存関係が破綻すると、ユースケースの追加や変更が非常に困難になります。

これはドメインのどちらが上流・下流を設計するのと同じ意味があります。ユースケースの上流・下流をきちんと設計することで、仕様変更時の影響を適切に管理することができるようになります。依存関係が循環していると、がっちり密結合した状態になってしまって、どちらに手を入れても、どちらにも影響が発生する状況になりがちです。このため基本的にユースケースごとに独立したコンポーネントとすることで、依存関係を制御します。

ただしViewだけは例外とします。これはViewのスタイルやテーマを個別に分割していくと、メンテナンスが大変になったり、デザイナーでプレビューが正しく表示されなかったり、リソースディクショナリのロードが重くなったり・・・問題がおおくて私自身うまく解決できていない為です。何かいい方法があれば、ぜひ教えてください。

では具体的にどうするか？ということで、いったん次のようにしてみました。

![](Article03/スライド45.PNG)

メニューのViewModelが含まれるPurchasing.ViewModelから、要再発注製品一覧画面のViewModelを呼び出して画面遷移します。そのコンポーネントがRePurchasing.ViewModelです。

要再発注製品一覧画面では、RePurchasingコンポーネント内のユースケースオブジェクトを利用して画面を表示します。

その際、サーバーサイドを呼び出して必要な情報を取得します。そのクライアントはRePurchasing.MagicOnion.Clientに含まれています。

クライアントはサーバーのMagicOnionで実装されたWeb APIを呼び出します。そのクライアントとサーバー間のインターフェイス定義がRePurchasing.MagicOnionに含まれています。

悪くなさそうですね。ではシナリオにしたがって設計・実装してみましょう。

#### 要再発注製品一覧画面に遷移する

```cs
[Navigate]
public partial class MenuViewModel
{
    private readonly RePurchasing.ViewModel.IPresentationService _presentationService;

    public MenuViewModel(
        [Inject] RePurchasing.ViewModel.IPresentationService presentationService)
    {
        _presentationService = presentationService;
    }

    [RelayCommand]
    private Task NavigateRePurchasingAsync() => 
        _presentationService.NavigateToRequiringPurchaseProductsAsync();
}
```

メニュー画面で再発注ボタンを押下したとき、ViewModelのCommandを呼び出します。Commandの実装は多用な方法がありますが、今回はCommunityToolkit.Mvvmというライブラリを利用します。

CommunityToolkit.Mvvmでは上述のようにRelayCommand属性を付与することで、自動的にCommandを生成してくれます。非常に便利なのでぜひ利用してみてください。

#### 再発注の必要な製品を表示する

再発注一覧画面は画面全体とは別にグリッド部分をユーザーコントロールとして実装します。このコントロールはSPREAD for WPFのデザイナーを利用して作成します。

![](Article03/スライド46.PNG)

SPREAD for WPF自体、Excelライクなグリッドを実装するためのコンポーネントですが、見てのとおりデザイナーもExcelライクな操作感になっています。比較的容易にグリッドをデザインできます。

WPFなのでプレゼンテーションはMVVMパターンを適用して実装します。View側からはViewModelをバインドしますが、このときXAML上でVisual StudioのIntelliSenseを利用してバインドするため、デザイン時ViewModelを適用することを強くオススメします。そうしておかないと、バインド時のプロパティ指定ミスが地味に多くなるからです。

```xaml
<UserControl ・・・
             d:DataContext="{d:DesignInstance {x:Type local:RequiringPurchaseProductsDesignViewModel}, IsDesignTimeCreatable=True}"
             mc:Ignorable="d" 
             d:DesignHeight="450" d:DesignWidth="800">
```

こんな感じでXAML側にRequiringPurchaseProductsDesignViewModelを指定します。デザイン時のViewModelは、私はxaml.csにあわせて定義しています。他で利用しないためです。デザイン時ViewModelはコード生成を検討したほうが良いかもしれません。

```cs
using AdventureWorks.Business.Purchasing.RePurchasing.ViewModel;

namespace AdventureWorks.Business.Purchasing.View.RePurchasing;

public partial class RequiringPurchaseProductsPage
{
    public RequiringPurchaseProductsPage()
    {
        InitializeComponent();
    }
}

public class RequiringPurchaseProductsDesignViewModel : RequiringPurchaseProductsViewModel
{
    public RequiringPurchaseProductsDesignViewModel() :
        base(default!, default!, default!)
    {
    }
}
```

実際のViewModelはDIを使う関係で、デフォルトコンストラクターは存在しない事が多いです。そのため、実際のViewModelを継承したデォルトコンストラクターをもつデザイン時ViewModelを作成してバインドして利用します。

さて、肝心の再発注対象の一覧をどのように取得しましょうか？

ドメイン駆動設計の場合、ドメインの登場オブジェクトを抽象化したエンティティクラスが存在します。しかし、要再発注製品一覧画面のグリッドで表示する1行を表すオブジェクトは、多数のエンティティから取得した情報を組み合わせたものになります。

![](Article03/スライド42.PNG)

この場合、必要な情報をエンティティのまた取得してきて、プレゼンテーション層で組み立てるようにすると、一覧画面で必要となる性能がでないことも多いです。

そこでCQRSパターンを活用します。

CQRSは、「コマンドクエリ責務分離」の略で、データベースに対する読み取り操作（クエリー）と書き込み操作（コマンド）を別々に扱うアーキテクチャのことです。

書き込み側の操作はDDDのエンティティを利用しますが、ユースケース時の複雑な情報の取得は読み取り専用のクエリーオブジェクトを利用します。データベースの読み取り操作には最適化されたクエリを使用することで、パフォーマンスの改善を期待できますし、プレゼンテーション層の実装も容易になります。またクエリーオブジェクトは、エンティティをユースケース視点で抽象化したものなので、実際のデータの持ち方を隠蔽できます。

私個人の経験として実際にそれで救われたことがあります。

私は業務アプリケーションでRDBを利用する場合、まずは基本的に、十分に正規化された形で保持するように設計します。そのように設計すると、複雑な検索が存在する場合に、多数の結合を利用した複雑なクエリーが必要になることがあります。そして、システムの利用が拡大した結果、どうしても十分な性能が出ない状態になりました。

そこでデータを保存する際に、正規化されたデータとは別に、検索に適した形で非正規化したテーブルを作っておき、検索時に利用するように改修しました。

WPF側では、検索サービスにクエリーオブジェクトを利用していた結果、C#のプログラム改修を一切なしに、検索性能を改善することができました。

とうわけで要検索製品一覧画面のレコードは、エンティティではなく、RePurchasingコンポーネント内に、RequiringPurchaseProductsQueryクラスを作成します。

```cs
public record RequiringPurchaseProduct
{
    public RequiringPurchaseProduct(
        VendorId VenderId,
        string VendorName,
        ProductCategoryId ProductCategoryId,
        string ProductCategoryName,
        ・・・
```

プロパティが多すぎるので、省略しますが、雰囲気は伝わるとおもいます。フラットな構造になっています。

そしてエンティティに対するリポジトリーに該当する、クエリーオブジェクトに対するクエリーサービスをPurchasingコンポーネントに作成します。

```cs
namespace AdventureWorks.Business.Purchasing.RePurchasing;

public interface IRequiringPurchaseProductQuery
{
    Task<IList<RequiringPurchaseProduct>> GetRequiringPurchaseProductsAsync();
}
```

そしてこのクエリーサービスのMagicOnion実装を、Purchasing.MagicOnion.Clientに定義します。

```cs
public class RePurchasingQueryClient : IRePurchasingQuery
{
    ・・・
    public async Task<IList<RequiringPurchaseProduct>> GetRequiringPurchaseProductsAsync()
    {
        var client = _clientFactory.Create<IRequiringPurchaseProductQueryService>();
        return await client.GetRequiringPurchaseProductsAsync();
    }
}
```

MagicOnionを利用するには、それに適したインターフェイスを定義する必要があります。それが_clientFactoryから生成しているIRequiringPurchaseProductQueryServiceです。

IRequiringPurchaseProductQueryServiceはクライアントとサーバー間で共有するインターフェイスなので、Purchasing.MagicOnionに定義します。

では、ここまでの実装をモデルに反映します。

![](Article03/スライド47.PNG)

#### 再発注画面に遷移する

ベンダーを選択肢、発注ボタンを押下されたら、再発注画面に遷移します。

再発注画面ではベンダーの詳細情報が必要になります。しかし要再発注製品一覧画面のクエリーモデルには、ベンダーIDと名称くらいしかもっていません。

ベンダーは画面遷移前と遷移後と、どちらで取得することもできますが、ここでは画面遷移前に取得します。遷移後の画面ではベンダーの情報を表示するために、ViewModelのベンダー情報をViewでバインドします。

遷移後で取得した場合、遷移後のViewModelのプロパティでnullを許可する必要があります。

逆に、遷移前に取得してから渡す場合、コンストラクターで注入することができるため、nullは不許可に設計できます。

そのため、遷移前に取得してから渡すことにします。

```cs
[Navigate]
public partial class RequiringPurchaseProductsViewModel : 
    INavigatedAsyncAware
{
    [RelayCommand]
    private async Task PurchaseAsync()
    {
        var vendor = await _vendorRepository.GetVendorByIdAsync(_selectedRequiringPurchaseProduct!.VendorId);

        await _presentationService.NavigateToRePurchasingAsync(vendor);
    }
}

```

遷移Commandは、メニューからの遷移と同様にRelayCommandを利用して生成します。やはり便利です。とくに非同期メソッドも何も考えずに利用できるのは、非常に便利です。

さて、Vendor関連を実装ビューに反映しましょう。

![](Article03/スライド48.PNG)

悪くないですね。

#### 選択されたベンダーと再発注が必要な製品の一覧を表示する

![](Article03/スライド43.PNG)

この画面では、ベンダーの情報と、再発注が必要な製品の一覧を表示します。

この画面でアーキテクチャ的に重要なポイントは、プルダウンコントロールくらいでしょうか。

プルダウンもなかなか奥深いコントロールです。業務アプリケーションのプルダウンとなると、個人的には検索可能なプルダウンはどうしても欲しいと思います。そうなるとやはり自作は大変です。

ということで、やはりサードパーティのコンポーネントスイートを利用した方が良いでしょう。

#### 発注する

さて、発注するにあたって、要再発注製品一覧画面のクエリーオブジェクトから、発注オブジェクトを作成する必要があります。ここでドメイン駆動設計における集約の悩みが生まれてきます。

下図は、左がデータモデルで、右が今回設計したドメインモデルです。

![](Article03/スライド49.PNG)

どちらも5つのオブジェクトが登場します。細かい名前は、それぞれのお作法や視点がやや異なるためです。

|データモデル|ドメインモデル|説明|
|--|--|--|
|Vendor|Vendor|製品の発注先ベンダー|
|ProductVendor|VendorProduct|製品の取り扱いベンダー。発注ドメインから見た場合、Vendorの子オブジェクトなので名前を入れ替えた。|
|Product|Product|製品|
|PurchaseOrderHeader|PurchaseOrder|発注。ドメインモデル側でHeaderがないのは、一般的に集約的なオブジェクトがあった場合、ルートオブジェクトは集合全体を扱う名称にすることが多いため。|
|PurchaseOrderDetail|PurchaseOrderDetail|発注明細|
|ShipMethod|ShipMethod|支払方法|

さて最大の違いは見ての通り、オブジェクト間の関係です。

データモデルは、RDBであるSQL Serverのサンプルデータベースですから、リレーションモデルで設計されています。十分に正規化されており、関連するオブジェクト間の関係はすべてリレーションで表現されています。

ドメインモデルは、集約の単位でリレーションが分割されています。ドメインモデルではリレーションで表現されていたけど、ドメインモデルではリレーションで表現されていない箇所は、IDで表現されています。ひとつながりのオブジェクトの範囲をドメイン駆動設計では集約と呼びます。

オブジェクト指向設計でRDBと同じリレーショナルモデルを表現した場合、巨大になりすぎてしまいます。

もちろん遅延リードのような仕組みを利用することもできます。しかし、遅延リードのような仕組みは、いつ遅延リードが行われるのか、透明性が失われます。遅延リードの実行有無によってオブジェクトの一貫性が損なわれることもあります。また遅延リードを行うという事は、エンティティ内にリポジトリーの仕組みを含まることになります。これはエンティティを複雑化し、エンティティがドメインロジックの本質に注力することが難しくなります。

このような理由から遅延リードのような仕組みは基本的に用いず、集約を設計することが推奨されています。

ではデータモデルのような連続的なリレーショナルモデルから、集約のような不連続のモデルを設計するとき場合、どのような指針に基づくべきでしょうか？

オブジェクト指向において、オブジェクト間の関連は大きく次の3種類に分けられます。

|関係|説明|
|--|--|
|関連 (Association)|オブジェクト間の一般的な関係を示します。関連は、通常、一つのオブジェクトが別のオブジェクトを使用する場合に発生します。関連は双方向または単方向であり、一対一、一対多、多対多などのカーディナリティを持つことができます。図上の実践で合わらず。|
|共有集約 (Aggregation)|オブジェクト間の"全体-部分"関係を表します。この関係では、あるオブジェクトが他のオブジェクトの集合を持ち、それらをグループ化しています。ただし、部分オブジェクトは独立して存在でき、複数の集約オブジェクトと共有されることがあります。共有集約では、全体オブジェクトが破棄されても部分オブジェクトは存続します。図上で親側に白抜きの菱形のついた実践で表す。未登場。|
|コンポジション (Composition)|より強い"全体-部分"関係を表します。これは、あるオブジェクトが他のオブジェクトの集合を持ち、それらをグループ化している点では共有集約に似ていますが、部分オブジェクトは全体オブジェクトに完全に依存しています。全体オブジェクトが破棄されると、部分オブジェクトも自動的に破棄されます。この関係は独占的で、部分オブジェクトは一つの全体オブジェクトにのみ属します。図上で親側に黒塗りの菱形のついた実践で表す。|



下図は、さきほどのオブジェクトの概念を純粋にモデル化したものです。

![](Article03/スライド50.PNG)

発注はベンダーに対して行われ、ベンダーが存在しなければ、その発注は発生しません。同様に発注明細も発注の一部です。

発注と配送方法の間には関連があります。しかし、どちらがどちらの一部とは言えません。

ベンダーと製品の関係も同様ですが、それらの間にはベンダーがどのような製品を取り扱っているのか表す、ベンダー製品という概念が存在します。

発注明細は製品が存在しなければ発生しませんが、製品からみると発注明細は、製品の一部ではないため、関連として表現しています。

これらを集約として分割していくにあたり、私はつぎのような指標を設けています。

|関係|説明|
|--|--|
|関連 (Association)|集約に含めない|
|共有集約 (Aggregation)|集約に含めない|
|コンポジション (Composition)|基本的に含めるが、つぎの2つは含めない<br>1. マスター・トランザクションのようなライフサイクルが大幅に異なるオブジェクト<br>2. 画像や動画のような大きなバイナリー|

この指標にしたがって分割した結果が下図の通りです。

![](Article03/スライド51.PNG)

さて、ここで私が一番悩んだのは、発注オブジェクト（PurchaseOrder - PurchaseOrderDetail）をどのように組み立てるか？とうことです。PurchaseOrderオブジェクトを作って、そこにPurchaseOrderDetailを1つずつ追加していくのがもっとも直感的ではあります。しかし実はそれは困難です。

一番分かりやすいのは、配送料（Freight）です。

配送料は、発注対象の総重量に配送方法のグラムあたりの単価である配送レートを掛けたものです。そのため配送料は、発注明細が追加されるたびに再計算する必要があります。しかし、発注オブジェクトには配送方法は含まれないため、配送料を計算することができません。

一番簡単な解決策は、配送ボタンを押下された際に呼ばれるViewModelで、配送明細をすべて作成して、配送料を計算してから配送オブジェクトを生成する方法です。しかしそうしてしまうと、ViewModel側にビジネスロジックが漏れ出してしまします。何のためのドメイン層なのかということになってしまいます。

そこで今回はビルダークラスを作成して解決することとしました。コードを見ながら説明しましょう。

まずはビルダークラスを使うコードです。

```cs
// ビルダー生成に必要なオブジェクトを集めて、発注ビルダーを生成する
EmployeeId employeeId = ...
Vendor vendor = ...
ShipMethod shipMethod = ...
Date orderDate = ...
var builder = new PurchaseOrderBuilder(employeeId, vendor, shipMethod, orderDate);

// 要再発注製品の一覧
IEnumerable<RequiringPurchaseProduct> requiringPurchaseProducts = RequiringPurchaseProducts;
foreach (var requiringPurchaseProduct in requiringPurchaseProducts)
{
    var product = await _productRepository.GetProductByIdAsync(requiringPurchaseProduct.ProductId);
    // 製品と数量を指定して明細を追加する。
    builder.AddDetail(product, requiringPurchaseProduct.PurchasingQuantity);
}

// 発注をビルドする
PurchaseOrder purchaseOrder = builder.Build();
```

ビルダークラスは、発注オブジェクトを生成するために必要な情報を集めて、最後にBuildメソッドを呼び出すことで、発注オブジェクトを生成します。

その際にビルダークラスに配送方法を渡しておくことで、ビルド時に配送料を計算することが可能になります。なお下記のコードは問題にフォーカスするため、実際のコードとはやや異なります。

```cs
private readonly List<(Product Product, Quantity Quantity)> _details = new();

public void AddDetail(Product product, Quantity quantity)
{
    _details.Add((product, quantity));
}

public PurchaseOrder Build()
{
    DollarPerGram shipRate = _shipMethod.ShipRate;
    Gram totalWeight = _details
        .Select(x => x.Product.Weight * x.Quantity)
        .Sum();
    Dollar freight = shipRate * totalWeight;
```

やっとバリューオブジェクトの価値を説明することができるようになりました。

配送料は、発注対象の総重量に配送方法のグラムあたりの単価である配送レートを掛けたものです。

このとき配送レートや重量、金額はintはdecimalといった .NETで提供されるプリミティブ側を利用して実装することもできます。しかしその場合、誤って別の数字を計算にもちいてしまってバグが発生する可能性があります。

そこで重量単価（DollarPerGram）、グラム、ドルといったバリューオブジェクトを作成して、それらを使って計算することで、誤って別の数字を計算にもちいてしまうことを防ぐことができます。

とくに業務アプリケーションの場合、多様な数値を扱います。このときにプリミティブ側を利用してしまうと、どの数値がどのような意味を持っているのかが分かりにくくなってしまいます。そこでバリューオブジェクトを利用することで、数値の意味を明確にできます。

たとえばグラムは足し引きはできますが、掛け算はできません。しかしグラムをintで扱ってしまうと、掛け算を防ぐことはできません。そういったドメインロジックを値毎に明確に実装できることがバリューオブジェクトの価値です。

業務アプリケーションの場合、本当に多様なバリューオブジェクトが発生してきます。本稿ではバリューオブジェクトの実装にUnitGeneratorライブラリを利用しています。非常に便利なライブラリなのでオススメです。

こうしてビルドしたPurchaseOrderオブジェクトはリポジトリーを利用して登録して、発注を完了します。

このあたりのドメイン駆動的な設計は、ユースケースの実現を通すことで見えてきます。

#### 発注が成功したことを、メッセージダイアログで表示する

アラートの表示はKamishibaiでViewModel側から利用する仕組みが用意されているので、これを利用します。

```cs
_presentationService.ShowMessage(Purchasing.ViewModel.Properties.Resources.RegistrationCompleted);
```

#### 要再発注製品一覧画面に遷移する

要再発注製品一覧画面に遷移するには、KamishibaiのIPresentationServiceを利用して、前の画面に戻ります。

```cs
await _presentationService.GoBackAsync();
```

#### 再発注の必要な製品を表示する

要再発注製品一覧画面を最初に遷移してきたときは、INavigatedAsyncAwareのOnNavigatedAsyncを実装して、再発注の必要な製品を表示しました。INavigatedAsyncAwareは画面が前に遷移したときに呼ばれるインターフェイスです。

戻ってきたときはIResumingAsyncAwareを利用します。

```cs
[Navigate]
[INotifyPropertyChanged]
public partial class RequiringPurchaseProductsViewModel : 
    INavigatedAsyncAware,
    IResumingAsyncAware
{
    ・・・
    public async Task OnResumingAsync(PreBackwardEventArgs args)
    {
        RequiringPurchaseProducts.Replace(
            await _requiringPurchaseProductQuery.GetRequiringPurchaseProductsAsync());
    }
```

実際の処理自体は変わりません。

これで「再発注する」ユースケースが実装できました。これでデータソーサのCRUDを実現する目途が立ちました。

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

たの2つのブランチ戦略との違いの1つにdevelopブランチの有無があります。リリースサイクルが長めの開発では、mainブランチとdevelopブランチが分かれていると扱いやすいと考えています。

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



# おわりに

さて要件定義編からここまで、さいごまでお付き合いいただきありがとうございました。

私自身、意気揚々とこの原稿を書き始めたものの、実のところ書いてみるとこれまで自分の開発してきたシステムのアーキテクチャにおいて、検討が不十分と思われる箇所に気が付くことがおおいく非常に難産となった記事となりました。

やや粗削りな箇所もあるかもしれませんが、その分おおくのアイディアや知見が詰まっていると思います。

本稿では、詳細まで書きすぎてもむしろ混乱を招いてしまうと思われる点について、意図的に省略して記載した箇所もあります。

動作するすべてのコードはGitHub上にあります。ぜひGrape CityさんからComponentStudio Oneと、SPREAD for WPFの試用ライセンスを取得して、ご自身の手で動かしながらソースをご覧ください。

また、本稿の内容について、ご意見・ご感想・ご指摘などございましたら、ぜひTwitterの@nuits_jpまでお寄せください。可能な限りご回答いたします。

では、また次回どこかでお会いしましょう。

