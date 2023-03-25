2022年版実践WPF業務アプリケーションのアーキテクチャ【設計編　前編】 ～ ドメイン駆動設計＆Clean Architectureとともに ～

# リード文（400字以内）

先日、「[2022年版実践WPF業務アプリケーションのアーキテクチャ【見積編】～ドメイン駆動設計＆Clean Architectureとともに～](https://codezine.jp/article/detail/16953)」という記事を公開させていただきました。こちらの記事では、見積時に考慮が必要なアプリケーションアーキテクチャの着眼点や、その具体的な検討内容について記載しました。

今回はいよいよ「設計編　機能要件の実現」ということで、見積が承認され、開発が開始して以降のフェーズに入ります。

開発が開始して以降、アーキテクチャに対して影響を与える要件として「機能要件」と「非機能要件」の2つがあります。これら2つの要件からはじまって、アーキテクチャを設計していくひとつの方法をお伝えしたいと思います。


# 前提条件

本稿はWPFアプリケーションのアーキテクチャ設計について記載したものです。

本稿にはサーバーサイドの設計も一部含まれていますが、見積編にも記載した通り、サーバーサイドについてはWPFアプリケーションを設計する上で、必要最低限の範囲に限定しています。サーバーサイドの実現方式は、オンプレ環境なのかクラウド環境なのか？といった容易などで大きく変わってきます。そしてWPFアプリケーションから見た場合には本質的な問題ではありません。サーバーサイドまで厳密に記載すると話が発散し過ぎてしまい、WPFアプリケーションのアーキテクチャにフォーカスすることが難しくなるため、あくまで参考程度にご覧ください。

また本稿ではAdventureWorks社の業務のうち、発注業務システムのアーキテクチャとなります。特定業務は発注業務に限定されますが、認証などの複数の業務にまたがったアーキテクチャの実現についても言及します。

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
# アーキテクチャ設計の構成

過去に記載した「[実践WPF業務アプリケーションのアーキテクチャ【概要編】 ～ マイクロソフト公式サンプルデータベースAdventureWorksを題材に](https://www.google.com/url?sa=t&rct=j&q=&esrc=s&source=web&cd=&cad=rja&uact=8&ved=2ahUKEwijj-XDpvD9AhVtS2wGHVCWACcQFnoECB4QAQ&url=https%3A%2F%2Fcodezine.jp%2Farticle%2Fdetail%2F10727&usg=AOvVaw0nFMOXzm1dpqOjvMuyfoHA)」では、アーキテクチャをRational Unified Process（RUP）にて提唱された4＋1ビューを用いて記載・説明しました。

![](/Article02/スライド20.PNG)

ただ4+1ビューではつぎのような点で設計が導きにくいと感じてきました。

- ドメイン駆動設計との統合
- 非機能要件
- データの永続化手段と利用方法
- バージョン管理やCI/CD

これらを踏まえると、つぎのような視点（ビュー）でアーキテクチャを表現するのが良いと考えています。

|No.|ビュー|説明|おもなモデル|
|--|--|--|--|
|1|ドメインビュー|境界付けられたコンテキストとコンテキストマップを用いてドメインとコンテキストを設計する。|境界付けられたコンテキスト、コンテキストマップ|
|2|ユースケースビュー|アーキテクチャを決定するための代表的なユースケースを選択・設計する。|クラス図、シーケンス図|
|3|非機能要件ビュー|アーキテクチャに影響を与える非機能要件を特定・設計する。|クラス図、シーケンス図|
|4|論理ビュー|ソフトウェアの論理レイヤー構成を設計する。レイヤー内の代表的なオブジェクトを抽出し、依存関係を設計する。|パッケージ図、クラス図|
|5|実装ビュー|論理ビューで抽出された代表的なオブジェクトをコンポーネントに分割配置する。|コンポーネント図|
|6|配置ビュー|システム全体の論理ノード構成と、ノード上へのコンポーネントの配置を設計する。|配置図|
|7|データビュー|システムが扱うデータの永続化方法、利用方法を設計する。|ER図、クラス図、シーケンス図|
|8|プロセスビュー|並行性やパフォーマンス要件で特別な検討が必要と考えられるアーキテクチャを設計する。|アクティビティ図、シーケンス図|
|9|開発者ビュー|システムの開発プロセスやツールを設計する。バージョン管理、CI、自動テストなどを含む。|配置図、シーケンス図|

## ドメインビュー

アーキテクチャ設計書のドメインビューでは、境界付けられたコンテキストとコンテキストマップを用いてドメインとコンテキストを設計します。

予算編で仮の境界付けられたコンテキストを作成しましたが、まずは対応するコンテキストマップを作成します。コンテキストマップではコンテキスト間の関係の種類を明確にします。関係の種類には共有カーネルやカスタマー・サプライヤー、腐敗防止層などが含まれます。

関係の種類を考慮して、論理ビュー・実装ビュー・配置ビューを作成します。

## ユースケースビュー

4+1ビューが提唱されたRUPにおけるユースケースビューには、すべてのユースケースやアクターの抽出を含む、ユースケースも出るの完成ととれる意図が含まれていたと思います。

ただそこまでいくと実質的な要件定義となってしまいます。それがダメという訳ではないのですが、アーキテクチャ設計の一部として記載することに個人的には違和感があります。

利用者の要求から要件を開発し、その要件を満たすアーキテクチャを設計するという流れが個人的にはしっくりきます。まず利用者に提供すべき価値（機能・ユースケース）があり、それの手段としてアーキテクチャがあるはずです。そのためドキュメント体系としては、アーキテクチャ設計書の前にユースケース定義書（要件定義書）があるべきだと考えています。

ただアーキテクチャ設計書のユースケースビューには、ユースケースの実現を設計する役割もあります。

そこでユースケース定義書（要件定義書）によって設計されたユースケースを実現する上で、アーキテクチャ的なパターンを特定し、パターン別の実現方法を設計する役割をユースケースビューに残すこととしました。

## 非機能要件ビュー

従来の4+1ビューでは、機能要件に対する実現は明確に記載されていましたが、非機能要件の実現が考慮しきれていませんでした。

そこで、機能要件に対するユースケースビューと同様に、非機能要件に対する非機能要件ビューを追加しました。

非機能全体の定義は、ユースケースビューと同様に、非機能要件定義書のような文書で定義されている前提とします。

非機能要件には、たとえば保守・運用に関する要件なども含まれます。そのため非機能要件ビューでは、定義ずみの非機能要件からアーキテクチャ上考慮が必要な要件を特定し、その実現方法を設計します。

## 論理ビュー

システムを実現するための代表的なオブジェクトを抽出し、それらを配置する論理的なレイヤー構成を決定します。

他のビューでも同様ですが、論理ビューに記載する内容は、論理ビュー内の設計ですべて完成させることはできません。とくにユースケースや非機能要件の実現を設計する中で新しいオブジェクトが登場してきます。そのため複数のビューを行ったり来たりしながら設計を進めていくことになります。

## 実装ビュー

論理ビューで抽出されたオブジェクトを、どのようにコンポーネントに分割配置するか設計します。

つまりWPFの場合、Visual Studio上のプロジェクト（.csproj）をどのように分割して、どのクラスをどのプロジェクトに配置するのか決定します。

.NETの場合、プロジェクトの分割によって厳密な依存関係を規定できるため非常に重要です。安易にプロジェクトを統合してしまうと、すぐに好ましくない依存関係を実装してしまいがちだからです。

ViewとViewModelで考えると非常に分かりやすいでしょう。

MVVMパターンで設計する場合、ViewはViewModelに依存しますが、その逆をしてしまうと循環参照になってしまいます。しかし細かな実装をしていると、ついついViewModelからViewを操作してしまいたくなります。それで正しく動作することもありますが、WPFの場合はListなどが仮想化されている関係上、Viewを直接操作してしまうと不具合のもとになることもあります。また単純に依存関係が双方向になると、コードが追いきれないきれいスパゲッティなコードになって、後日のメンテナンスで苦労しがちです。

そのため依存関係を適切に制御するため、プロジェクトをどう分割するかは、非常に重要な設計になります。

## 配置ビュー

どのノードに、どのコンポーネントを配置するか設計します。

このとき実装ビューで抽出したコンポーネントだけでなく、サードパーティのライブラリーやランタイム・OS・ミドルウェアなども記載します。

ノードは論理的なノードとして扱い、物理的なノード設計は別途詳細な設計を用意します。物理的な設計はインフラの詳細な設計にフォーカスするためです。

## データビュー

システムが扱うデータの永続化方法、利用方法を設計します。

- 永続化先はファイルシステムなのか？RDBなのか？NoSQLなのか？
- RDBだとしたらスキーマをどのように設計するのか？接続時のユーザーはどのように割り当てるのか？
- RDBをどのように利用するのか？Entity Frameworkか？Dapperか？

そういった内容を設計します。

データベース全体のER図のような、詳細な設計は含めず、別途データベースの詳細設計書などに記載します。

## プロセスビュー

並行性やパフォーマンス要件で特別な検討が必要と考えられるアーキテクチャを設計します。

ほとんどの場合は、.NET（async/awaitなど）やASP.NET Coreなどがになってくれるため、それらを単純に使うだけなら特別な設計は不要です。

今回のケースではWPFのプロセスを起動する際のDependency Injection（DI）コンテナーの初期化に関連する設計が必要になります。

## 開発者ビュー

システムの開発プロセスやツールを設計します。

- IDEには何を使うのか？
- ユーザーの開発環境に必要なランタイムやミドルウェアはなにか？
- バージョン管理には何を使うか？
- Gitを使うとした場合、そのブランチ戦略は？
- Unit Testフレームワークには何を使うか？
- CIはどのように行うか？
- CI時の自動テストは？

そういった日々の開発者体験に直結するプロセスやツールを決定します。ある意味では開発者にとって一番大切な部分です。

# 設計編の構成

前述の構成は、アーキテクチャ設計書としては読みやすいと思います。

しかし実際にアーキテクチャ設計を行っていく場合、各ビューを頻繁に行ったり来たりしながら記述します。いずれかを先に完璧に書きあげるという訳には行きません。

たとえば論理ビューから実装ビュー・配置ビューは概ねその方向に流れて設計しますが、論理ビュー自体がユースケースビューや非機能要件ビューの設計に伴い頻繁に更新されるため、ウォーターフォール的な流れにはならず、インクリメンタルなプロセスになります。

本稿では「アーキテクチャ設計書はこうなります」という設計結果をお見せするのではなく、どのようにアーキテクチャを設計していくか解説したいと考えています。そのため設計書としてのアーキテクチャ設計とは、やや異なったアプローチで記載します。

そこで設計編では、つぎの構成で記載していきたいと思います。

1. 前編
   1. 初期ドメインビューの設計
   2. 初期ユースケースビューの設計
   3. 初期非機能要件ビューの設計
   4. 初期配置ビューの設計
   5. 初期論理ビューの設計
   6. データビューの設計
       1. 初期実装ビューの設計
   7. プロセスビューの設計
2. 後編
   1. 代表的なユースケースの実現
   2. 非機能要件の実現
   3. 開発者ビューの設計

## 前編

本稿、前編ではまずはざっくりしたアーキテクチャの概略を設計します。

この段階ではあまり正確なものを作ることに拘る必要はありません。正確なアーキテクチャはすべてのユースケースや非機能要件が実現されるまで完成しません。

そのため、まずは後編に記載があるような代表的なユースケースの実現に着手できる状態とします。速度を優先し、正確性はある程度目をつぶりましょう。

これはいい加減で良いという意味ではありません。とくに類似のアーキテクチャに対する経験が多い方は、この段階でかなり正確な設計が可能です。ただ、悩んで何日も手が止まってしまうくらいなら、先に進めてからフィードバックすれば良いと思います。

## 後編

後編ではユースケースや非機能の実現を設計します。

その中で、各ビューにフィードバックしていき、アーキテクチャ全体の精度を上げていきます。

ユースケースの実現は、必ずしもすべてのユースケースを同じ粒度でアーキテクチャ設計書に記載する必要はありません。ユースケースをアーキテクチャ的な視点でパターン分けして、同一パターンの中から代表的なユースケースを選定します。その代表的なユースケースに絞って記載する形とします。

それに対して非機能要件は、アーキテクチャに影響があるすべての非機能要件について設計する必要があります。ただ紙面の都合もありますので、今回は普遍的に活用できそうないくつかの非機能に絞って設計したいと思います。

そして最後に開発者ビューを設計します。本稿の構成上最後に記載しますが、実際には最後に書かないといけないという訳ではありません。書けるタイミングで順次記載していき、開発上必要になるタイミングまでに完成させれば良いかと思います。

では！いってみましょう！
# 初期ドメインビューの設計

本章では購買ドメインのドメインビューを設計します。

ドメインビューではつぎの2つのモデルを設計します。

1. 境界付けられたコンテキスト
2. コンテキストマップ

境界付けられたコンテキストを利用して、購買ドメインを中心とみたときに、関連するドメイン・コンテキストを抽出して、それぞれのドメインがどのような役割を持つのか設計します。

そこで抽出されたコンテキスト間の関係を、コンテキストマップをつかって設計します。

## 境界付けられたコンテキスト

予算編で記載したように、Adventure Works Cycles社全体の境界付けられたコンテキストは下記のとおりです。

![](/Article02/Domain_Model_01.png)

Adventure Works Cycles社はワールドワイドな自転車製造・販売メーカーです。そのためビジネス全体をみたとき、コアとなるのは販売ドメインです。販売ドメインを提供するために、購買・製造・配送ドメインが支援します。

ただし本稿の開発対象は購買ドメインです。

そのため、購買ドメインからみた境界付けられたコンテキストは下記のとおりです。

![](/Article02/スライド10.PNG)

予算編で記載したものとほぼ同じですが、下記の2点を変更しています。

1. 共通する概念としてAdventureWorksドメインとコンテキストを定義
2. 製造・販売コンテキストから認証コンテキストの依存線を削除

前者については、AdventureWorks全社に共通するオブジェクトを定義するコンテキストとして導出しました。

基本的に企業にとってプリミティブなオブジェクトを定義し、複雑なオブジェクトはそれぞれの業務ドメイン内で定義することも検討してください。詳細はコンテキストマップの中で説明します。

## コンテキストマップ

コンテキストが導かれたら、つぎはコンテキスト間の関係を整理します。

ドメイン駆動設計のコンテキストマップを利用して整理したモデルが下記のとおりです。

![](/Article02/スライド24.PNG)

コンテキスト間の関係を整理するためには、つぎの2つを明確にする必要があります。

1. 関係の向き
2. 関係の性質

矢印の向きが上流下流を表していて、矢印の向いている先が上流、矢印の根元が下流です。

コアとなる購買コンテキストと、それ以外のコンテキストの関係について順に整理しながら、それらをどう考えればよいか説明していきましょう。

## 販売コンテキストと製造コンテキスト

たとえば購買コンテキストで他社の部品などを発注する場合、どれだけ発注するべきか判断するためには、販売情報が必要です。つまり購買コンテキストは販売コンテキストに依存します。そのため購買コンテキストが下流で、販売コンテキストが上流になります。

ちなみに関係の向きは視点が変わると逆になる場合もあります。

たとえば販売コンテキストを中心に見た場合を考えましょう。商品を販売しようとしたときに、その商品が欠品していて販売できなかったとします。その際に、つぎの入荷予定がいつになるのか知りたい場合もあるでしょう。その場合は購買コンテキストから購買情報を取得する必要があります。そのため、販売コンテキストを中心に見た場合、購買コンテキストが上流になります。

関係の向きは固定ではないため、注意が必要です。

関係が決まったらその性質を決定します。ドメイン駆動設計ではつぎのような関係の中から、いずれの関係に該当するか決定します。

1. 共有カーネル
2. カスタマー・サプライヤー
3. 順応者
4. 腐敗防止層
5. 別々の道
6. 公開ホストサービス

関係の性質はこれだけではありませんし、既存の性質で表現できない場合は、あたらしく定義してもかまいません。ただ多くの場合は上記のいずれかから選べば十分でしょう。

さて、購買コンテキストと販売コンテキストを見た場合、どういった関係になるでしょうか？

購買コンテキストと販売コンテキストの開発は平行に行われます。スケジュールなどにつねに余裕があるとは限りません。あまり密に結合していると、販売コンテキストの変更に購買コンテキストが、追随できない可能性があります。またリリース後に販売コンテキストの改修が入った場合、購買コンテキストへの影響はできる限り限定したいところです。

そもそも購買コンテキストで必要な販売情報は、販売コンテキストほど詳細な情報は必要ありません。

このような場合、販売コンテキストと購買コンテキストの「販売」オブジェクトは、別々に設計・実装したほうが良さそうです。

そのうえで、販売コンテキストで「販売」された場合、その情報を適宜変換して購買コンテキストに取り込み、購買コンテキストの「販売」オブジェクトとして扱うのが好ましいです。

つまり「腐敗防止層」の関係とします。

製造コンテキストについても同様です。

購買のためには、現在製造中の製品も考慮して必要な購買量を決定する必要があります。そのため製造コンテキストは購買コンテキストの上流となり、性質も腐敗防止層とするのが良いでしょう。

## AdventureWorksコンテキスト

さて、購買コンテキストと販売コンテキストにはどちらも「販売」オブジェクトが登場することを説明しました。販売オブジェクトは、一見共通のオブジェクトのように見えますが、購買コンテキストと販売コンテキストで必要になる属性や振る舞いが異なることから、それぞれのコンテキストに別々に定義することとしました。

しかし逆に直接共有したほうが好まいモデルやコードもあります。そういったオブジェクトをAdventureWorksコンテキストに定義します。

AdventureWorksコンテキストには、具体的にはつぎのようなオブジェクトを定義します。

|オブジェクト|説明|
|--|--|
|Date|時刻を持たない年月日|
|Days|日数|
|Dollar|通貨（日本企業の場合はYenなど）|
|Gram|重量グラム|
|DollarPerGram|グラム当たりの料金|

これらは少なくともAdventureWorks内ではプリミティブなオブジェクトで、直接共有したほうが生産性も品質も高めることができます。

これらのオブジェクトは、購買・販売・製造コンテキストの開発者で合意のもと協力して開発します。そのため「共有カーネル」という関係を選択しました。

これらのオブジェクトをドメイン駆動設計のValue ObjectやEntityとしてAdventureWorksコンテキストに実装します。複雑なオブジェクトは個別の業務ドメイン内に実装したほうが良いことため、ほとんどはValue Objectになるでしょう。

ただこれらも、変更容易性を優先する場合は、上記のオブジェクトも業務コンテキストにあえて定義する方式も考えられます。生産性と変更容易性はトレードオフの関係になりやすいです。この辺りは共通部分に破壊的変更が入りやすいかどうか判断したらよいと思います。

## 認証コンテキスト

認証はとくにセキュリティ上、非常に重要なコンテキストになります。そのため個別のコンテキストで実装することはリスクが高く、慎重に作られたものを共有することが好ましいと判断しました。

ただAdventureWorksコンテキストのように複数のコンテキスト間で共有して継続的に開発するというより、共通の仕様を規定して作られたコンポーネントをそれぞれが利用するという形をとることとしました。

そのため関係の性質としてはカスタマー・サプライヤーを選択しました。

# 初期ユースケースビューの設計

ユースケースとは、利用者にとってシステムを利用する価値を表し、1つ以上の機能の組み合わせによって提供されます。

ユースケースと機能は明確に異なります。たとえば注文を発注する際に、発送方法をプルダウンで選択するとします。これは明確に「機能」ですが、利用者の最終的な価値にはなりません。ユースケースとはあくまでも利用者に価値を提供するための、1つ以上の機能の集合を表すものとします。

私たちが実現すべきものは機能ではなく、ユーザーに提供する価値だと考えています。そのため機能ビューではなく、ユースケースビューとして扱います。

ユースケースビューは、ユースケース図を完成させることが目的ではありません。ユースケースは、ユースケース仕様書（一般的な機能定義書のレイヤーに類するドキュメント）で作成されているものとします。

ユースケースビューでは、システムのアーキテクチャを決定するためにパターンの導出と、アーキテクチャ設計を進める上で代表となるユースケースを選定します。そのうえで、代表的なユースケースをどのように実現するか設計します。

初期ユースケースビューの設計では、代表的なユースケースの選定までを行います。またアーキテクチャ設計は、要件定義と並行で行われることが多いため、すべてのユースケースが揃っている必要もありません。ドメイン内でとくに重要なユースケースが導出されていれば問題ないでしょう。

つぎのような表を用いると表現しやすいかと思います。

|No.|ユースケース|パターン|代表|説明|
|--|--|--|--|--|
|1|発注する|基本パターン|||
|2|再発注する|基本パターン|✅|アーキテクチャを検討する上で、基本パターンとして必要な内容が含まれており、かつ、最小のユースケースであるため|
|3|・・・|・・・|||
|4|・・・|・・・|||

ユースケースを一覧として記載し、それぞれのユースケースのパターンを抽出・割り当てます。類似したユースケースを同一のパターンに割当て、アーキテクチャの検討は、同一パターン内の代表的なユースケースを用いて設計します。

アーキテクチャを設計するにあたり、つねにすべてのユースケースを検討しつつ進めることは困難ですし、新たらしいユースケースが追加された場合に全面的に見なおすことも困難です。パターンを用いることでそれらの課題を緩和します。

そのうえで、この後のアーキテクチャを設計する際に利用する代表的なユースケースを決定します。抽象化されたパターンだけでは擬態的な検討が、不足する可能性があります。そのため、代表的なユースケースを決定することでアーキテクチャを具体化します。

代表的なユースケースは、パターンに必要な要素が含まれた最小のユースケースを選定することが好ましいでしょう。ただ、そう都合の良いユースケースが存在しない場合もあります。多少の大小は目をつぶり、一部不足するような場合は、不足部分を別途パターンとして抽出して、他のユースケースで補えば良いかと思います。

たとえば「再発注する」ユースケースには、複雑なクエリーや、発注データの登録が含まれます。CRUDのうちCreate（C）とReference（R）は含まれていますが、Update（U）とDelete（D）が含まれません。Createと比較してUpdateやDeleteに異なるアーキテクチャが必要なのであれば、それらを含むユースケースをUpdateパターンのように抽出することで補いましょう。

設計編の後編では「再発注する」ユースケースの実装を詰めていくことでアーキテクチャを設計します。

# 初期非機能要件ビューの設計

ユースケースビュー同様、非機能要件を定義するものではありません。非機能要件定義書などで定義された非機能要件のうち、アーキテクチャ上で考慮が必要な要件を明確にし、その実現方式を設計します。

非機能要件には、運用時の要件なども多く含まれます。たとえば障害発生時の対応可能時間（9時～17時）などです。

非機能定義書で定義された非機能要件のうち、アーキテクチャ上で検討するべき要件がどれか？不要な場合、なぜ不要なのかを明確にすることが非機能要件ビューの目的となります。

ここで非機能要件の定義について、詳しく記載することはかないませんが、たとえばIPAの公開している「[非機能要求グレード](https://www.ipa.go.jp/sec/softwareengineering/std/ent03-b.html)」をベースに不足があれば追加していくと、扱いやすいのではないでしょうか。

非機能要件のアーキテクチャ上の考慮要否を制する場合、つぎのような表を用いると表現しやすいかと思います。大項目から指標までは非機能要求グレードのフォーマットに則っています。

|大項目|中項目|小項目|指標|考慮|理由|
|:--|:--|:--|:--|:-:|--|
|可用性|・・・|・・・|・・・|||
|運用・保守性|通常運用|運用監視|監視情報|要|WPF上でのエラーとトレース情報を適切にサーバーサイドにログとして保管する。例外時にも抜け漏れのないログ出力を実現する。|
||障害時運用|システム異常検知時の対応|対応可能時間|不要|運用保守の体制にて実現し、アーキテクチャに影響はないため。|
|セキュリティ|アクセス・利用制限|認証機能|・・・|要|WPFアプリケーションの利用者を認証する。認証機能はドメイン共通のカスタマー・サプライヤー関係として提供する。そのため、本来は全機能共通のアーキテクチャ設計上で実施するが、便宜上本稿に記載する。|
|・・・|・・・|・・・|・・・|||

上記は非機能要件の一部の抜粋です。

考慮列には、非機能要件ごとにアーキテクチャ上の考慮が必要かどうか記載し、その理由を理由列に記載します。考慮の要否以上に、なぜ決定したかその理由の方が後々重要になるため、しっかり書き残しておきましょう。

後編では、例外処理を含めたログ出力と、認証の実現方法を設計します。

# 初期の配置ビュー

予算編で記載したように、購買システムはクライアント・Web API・データベースの三層アーキテクチャを採用します。また購買システムは販売ドメインと製造ドメインに依存するため、それらのノードも意識する必要があります。

購買ドメインの開発が開始しているということは、販売ドメインと製造ドメインの予算も決定されているはずで、購買ドメインの予算編と同等のアーキテクチャは決定されていると想定します。

それらを配置図に起こすとつぎのようになります。

![](/Article02/スライド21.PNG)

購買・販売・製造パッケージがあり、それぞれに論理的なノードと、ノード上に配置されるコンポーネントを記載しています。販売ドメインも製造ドメインも、購買ドメインと同様に、データの永続化にはSQL Serverを利用するものとします。

この時点でもう少し設計を詰められそうなのはつぎの2点でしょうか？

1. 購買APIのアーキテクチャ
2. 販売・製造ドメインとの関係性

## 購買APIのアーキテクチャ

購買APIをRESTでつくるのか？それとも別のものを利用するのか？といったアーキテクチャの選択は、この時点でできることが多いでしょう。逆にいうと、個別のユースケースによって変わるようなものではないとも言えます。

Web API実装の選択肢として、つぎのものを候補として検討します。

1. REST
2. gRPC
3. GraphQL

結論としては今回はgRPCを利用する想定で設計を進めます。理由はいくつかあります。

1. 購買ドメインでは、「発注する」ようなRPC（Remote Procedure Call）のようなスタイルのメッセージがあり、RESTのようなリソース要求スタイルか、RPCスタイルかのどちらか一方に寄せることで設計を簡略化するとした場合、gRPCに寄せたほうが素直な設計に感じる
1. 購買ドメインのWeb APIを直接外部に公開する想定はなく、RESTほどの相互接続性は必要ない
1. 同様にGraphQLほどの柔軟性も必要ない
1. RESTとgRPCでは単純にgRPCの方が軽くて速いことが多い
1. .NETにおけるgRPCでは[MagicOnionというOSS](https://github.com/Cysharp/MagicOnion)を利用することで、C#のインターフェイスベースでの設計・実装が可能で、RPCスタイルのデメリット（エンドポイントが分かりにくい）を解消できる

gRPCの実装にはMagicOnionを利用する想定ということで、モデルに追加します。

![](/Article02/スライド22.PNG)

MagicOnionは、クライアントとサーバーでそれぞれモジュールがことなるので、そのとおり記載しています。

## 販売・製造ドメインとの関係性

購買ドメインでは、販売・製造ドメイン上のオブジェクトを、購買ドメインに同期する必要があります。たとえば購買数を決定するにあたり、販売実績を参照するといった内容を実現するためです。

販売・製造ドメインとの関係性からアーキテクチャを設計する際、つぎの点を考慮する必要があります。

1. 販売・製造ドメインが上流である
2. 販売・製造ドメインとの関係性は腐敗防止層である

販売・製造ドメインは購買ドメインの上流にあたります。

そのため販売・製造コンテキストのオブジェクトを購買ドメイン側で解釈し、購買コンテキストのオブジェクトに変換して取り込みます。それらを販売・製造それぞれの腐敗防止層コンポーネントとして実装することにします。腐敗防止層コンポーネントはひとまず購買データベース上に配置しましょう。

![](/Article02/スライド26.PNG)

腐敗防止層の実装の詳細は、ユースケースビューで、それらの要件となるユースケースの実装を設計することで詳細化します。ただ腐敗防止層の実装はWPFのアーキテクチャから乖離しすぎるため、「2022年版実践WPF業務アプリケーションのアーキテクチャ」内では取り扱いません。

# 初期論理ビューの設計

## ドメイン駆動とクリーンアーキテクチャ

さてアプリケーション全体の構成を考えたとき、ドメイン駆動設計単独では、どのように論理レイヤーを構成し、どの役割のオブジェクトをどのレイヤーに配置するのか規定されていません。そこで活用したいのがクリーンアーキテクチャです。

ドメイン駆動設計とクリーンアーキテクチャは非常に相性が良い設計手法です。

ドメイン駆動設計で大切なのはもちろんドメインになります。このとき、たとえば通常の垂直レイヤーアーキテクチャを用いて設計した場合、上から下への一方通行の依存関係になります。

![](/Article02/レイヤーモデル.PNG)

つまり、ドメインがインフラへのアクセス層に依存する形となってしまいます。

基本的に依存関係は、重要度の低い方から、高い方に向いていることが好ましいです。これは重要度が低い箇所の変更影響を、重要度の高い箇所が受けないようにするためです。このあたりの課題は[筆者のクリーンアーキテクチャの解説](https://www.nuits.jp/entry/easiest-clean-architecture-2019-09)を一読いただければ、ご理解いただけます。

詳細は先の記事を見ていただくとして、クリーンアーキテクチャについて誤解されがちないくつかの点について先に説明しておきたいと思います。下図は有名なクリーンアーキテクチャの図です。

![](/Article02/CleanArchitecture.jpg)

クリーンアーキテクチャでもっとも大切なことは、アーキテクチャ上もっとも重要な要素を中央のレイヤーに配置して、依存関係はすべて外から中に向けるという点にあります。ただしそうすると、ドメインからリポジトリーの呼出しのような部分の実現が困難になります。そのため、右下の実装例のように制御の逆転を使うことで、依存性が中から外に向かわないようにしましょうというのが上記の図になります。レイヤー数や登場要素を、上記の図の通りにしましょうというアーキテクチャではありません。

さてドメイン駆動設計において、もっとも大切な構成要素はもちろんドメインです。そのためリポジトリーの実装の影響をうけるようなことは避けるべきです。リポジトリーの実装がドメインに依存するように設計するべきです。これを実現するために手段としてクリーンアーキテクチャが利用できます。中央のEntityをドメインと読み替えると、ほぼそのまま適用できるようになります。

ここであらためて、境界付けられたコンテキストを見なおしてみましょう。

![](/Article02/スライド10.PNG)

購買ドメインの上位にAdventureWorksドメインが存在します。AdventureWorksドメインは、認証・製造・販売ドメインからも利用される汎用的なドメインです。そのためEntityの部分を単純にドメインに置き換えるのではなく、AdventureWorksドメインと購買ドメインの2層に分けたほうが良さそうです。

またもっとも外側のFrameworks & Driversレイヤーの要素は、実際に今回のドメインで必要となるものを記載しましょう。

それらを反映した現在のレイヤーモデルはつぎのとおりです。

![](/Article02/スライド13.PNG)

## レイヤーアーキテクチャにおける選択

さてレイヤーアーキテクチャには2つの選択肢があります。

1. 厳密なレイヤーアーキテクチャ
2. 柔軟なレイヤーアーキテクチャ

厳密なアーキテクチャを選択した場合は、直下への依存しか許可しません。柔軟なレイヤーアーキテクチャを選択した場合は、相対的に下位のレイヤーであれば依存（利用）を許可します。厳密なレイヤーアーキテクチャを採用した場合、レイヤーをまたいだ内側を利用したい場合、ひとつ外側がそれをラップして隠ぺいする必要があります。たとえば今回であれば、プレゼンテーションがドメインを利用する場合、つねにユースケースでラップして隠ぺいすることになります。

厳密なレイヤーアーキテクチャの方が、内側の影響を受けにくくなるため保守性が向上し、柔軟なレイヤーアーキテクチャは内側を隠ぺいするコードが必要ないため、生産性が高くなります。

結論から言うと、今回は柔軟なレイヤーアーキテクチャを選択します。大きな理由が2つあります。

1. AdventureWorksドメインをラップしてしまうと、生産性や品質に対する影響が大きい
2. リポジトリーの実装などを考慮すると厳密なレイヤーアーキテクチャでは実現できない

### AdventureWorksドメインをラップしてしまうと、生産性や品質に対する影響が大きい

前述しましたが、AdventureWorksドメインにはつぎのようなオブジェクトを定義します。

|オブジェクト|説明|
|--|--|
|Date|時刻を持たない年月日|
|Days|日数|
|Dollar|通貨（日本企業の場合はYenなど）|
|Gram|重量グラム|
|DollarPerGram|グラム当たりの料金|

AdventureWorksドメインにおけるプリミティブな型をValue Objectとして実装するため、これらを一々ユースケースでラップすると生産性が低下しますし、ラップミスの発生もあり得るため、品質も低下します。

そもそも厳密にした場合、AdventureWorksコンテキストを共有カーネルにした意味が無くなってしまいます。

### リポジトリーの実装などを考慮すると厳密なレイヤーアーキテクチャでは実現できない

たとえば購買ドメインには購買先を表すVendorエンティティと、そのリポジトリーであるIVendorRepositoryインターフェイスを定義することになるでしょう。そして、VendorRepositoryクラスはゲートウェイに実装されます。

![](/Article02/スライド23.PNG)

VendorRepositoryクラスからIVendorRepositoryインターフェイスへの依存は、ユースケース層を跨いでいますが、さすがにここをユースケース層でラップするのは助長にすぎます。

というわけで今回は柔軟なレイヤーアーキテクチャを選択します。

## Frameworks & Driversレイヤー

さて外側が具象的で、内側がより抽象的な構造になっています。最も外側のFrameworks & Driversレイヤーは、アプリケーションから利用するフレームワークやミドルウェアのレイヤーで、開発対象外のレイヤーです。

ユーザーインターフェイスはWPF上に構築し、永続仮想としてはSQL Serverを利用します。またクライアントとデータベースの間にWeb APIを挟んだ三層アーキテクチャとしたいため、Web APIをMagicOnionで実現します。

クリーンアーキテクチャ本にも記載されていますが、抽象化とは具体化を遅延させるための手段でもあります。そのためこの時点で具体的なFrameworkやDriverを決定する必要はありません。

ただ現実的な話、開発がスタートしてアーキテクチャを設計する段階では、Frameworks & Driversレイヤーの実体は決定しているものが多いです。なぜなら見積に影響するため、見積時のアーキテクチャ設計で多くの場合、十分に検討した上で決定しているからです。

すでに実体が決定しているなら遠慮せずWPFやSQL Server、MagicOnionのように具体的な要素をプロットしましょう。そのことはアーキテクチャ設計を容易にする面もあるからです。

一番分かりやすいのはWPFでしょうか。WPFで実装する場合、とくに理由がなければMVVMパターンを採用するでしょう。MVVMパターンを前提に設計されたUIフレームワークだからです。

このように外側の詳細の決定によって、内側の設計が用意になることがあります。そのため最外周が決定した段階で具体的な名称を記載しておくと良いと思います。

これもちろん、抽象化を利用して具象の決定を遅らせることを否定するものではありません。

さて、これ以上は実際のユースケースを設計しながら進めたほうが良いでしょう。ということで、初期の論理ビューとしては、いったんこの辺りとしておきます。

# データビューの設計

データビューでは、データの永続化方法・利用方法を設計します。

とはいえ既に、データはSQL Serverに永続化することは予算編で十分検討した上で決定しています。

そこでこのビューでは、つぎの点について設計します。

1. データベース利用アーキテクチャ
1. ORMの選択

## データベース利用アーキテクチャ

さて、みなさんはデータベースオブジェクトを配置するためのスキーマや、データベース接続時のユーザーなど、普段どのように設計しているでしょうか？

本稿はWPFアーキテクチャの記事なので、あまり深く踏み込めませんが、つぎの2点を重要視してアーキテクチャを設計しています。

1. データベースオブジェクト変更の影響を、データベーススキーマ上だけで正しく判断できる
2. 他のユースケースによるオブジェクト（テーブル構造など）変更が、他のユースケースに波及しない

テーブル変更した場合の影響範囲を正しく把握しよとしたとき、C#のコードを精査しなくては把握できない場合、RDBと.NETのインピーダンスミスマッチが原因で毎回非常につらい思いをしてきました。そのため、つぎのように設計することで基本的にデータベースのスキーマ上だけで影響範囲を特定できるようにしています。

1. 接続ユーザーはユースケースごとに別ユーザーとする
2. 接続ユーザーにはユースケースを実現する上で最低限の権限を付与する

また特定のユースケースの変更のため、テーブルに新しい列が必要になったとします。その際に、そのテーブルを参照している別のユースケースへの影響がでるのも大変つらいです。そこでつぎのように設計しています。

1. テーブルは直接操作せず、ビュー越しに操作する
2. ビューは全く同じ構造でも、ユースケース別に作成する
3. ビューはユースケース専用のスキーマ上に作成する

ビューをデータベース上の抽象化レイヤーとして扱うことで、物理テーブル変更の影響を最小限で抑えるようににしています。

## ORMの選択

現在、.NETでRDBを操作する場合に利用するORMとしては実質2択でしょうか？

1. Entity Framework Core（以後EF）
2. Dapper

ドメイン駆動設計で永続化されるオブジェとは、ドメイン層のエンティティになります。

エンティティの永続化にはEFを利用されている方が一定数いることは認識していますが、私個人としてはDapperを利用しています。

最大の理由はドメイン層をフレームワーク非依存で実装したいためです。

あらためてレイヤーモデルを見てみましょう。

![](/Article02/スライド23.PNG)

Entity Framework Coreを利用する場合、つぎのいずれかで実装する必要があります。

1. DDDのEntity（上図のVendor）をEFのEntityとして実装する
2. EFをゲートウェイ（上図のVendorRepository）の中だけで利用し、ゲートウェイ内でDDDのEntityに詰め替える

前者はドメインがSQLサーバーに依存してしまい、依存は外側から内側だけという大原則に違反してしまいます。これはデータベース設計の変更が、アプリケーション全体に波及する可能性があるということで、可能な限り避けたいところです。

後者はというと、DDDのEntityとEFのEntityの両方を実装してつねに詰め替えるひと手間が増えてしまいます。正直なところEFを使うメリットがほとんど失われてしまうように感じます。またDapperで直接Entityを生成することに比較して、CPUもメモリーも多く消費する点も気になります。

また前節に記載した「データベース利用アーキテクチャ」をEFで守ろうとすると、データベースファーストでEFを利用する必要があり、EFを最大限活用することもできません。

これは私にとって身近なシステムの特性の問題が大きいため、EFをコードファーストで利用できるような環境であれば、EFを選択することは十分にメリットがあるのではないかと思います。私はEFに十分習熟しているとは言いかねるので、詳しい方の意見も伺ってみたいところです。

とにかく今回はDapperの利用を前提とします。

## データベース接続コードの設計

データベース利用アーキテクチャを実現しようとした場合、実装に少し工夫が必要です。

またデータベース接続の実装時に、単純にDapperだけだと痒いところに手が「届かない」箇所がいくつかあります。

1. IDコンテナー上で接続文字列を解決する方法が提供されていない
2. データベース接続コードやトランザクション制御コードがやや煩雑になる

とくに前者は、データベース利用アーキテクチャを実現しようとした場合に、複数のデータベースユーザーを使い分ける必要があります。もちろんユースケース別にASP.NETのプロセスを分けて、1つのプロセスで複数のユーザーを使い分けないという方法もありますが、アーキテクチャ的にそれを制約とはしたくありません。アーキテクチャ的には1プロセスNユーザーを可能にしておきたいです。

そこでデータベースを抽象化して、下図のような設計にしたいと思います。

![](/Article02/スライド28.PNG)

Databaseパッケージに接続文字列の解決や、データベース接続、トランザクション制御を共通化して実装します。

IDatabaseの機能的な実装はDatabaseクラスで実装しますが、これをそのまま使うと、同一のDIコンテナー上で異なるデータベースユーザーを使い分けられません。

そこでDatabaseクラスは抽象クラスにしておいて、直接利用できないようにします。その上で、ユースケース単位でDatabaseクラスの実装クラスを容易します。

上図では発注（Purchasing）ユースケースと再発注（RePurchasing）でそれぞれDatabaseの実装クラスを用意しています。これらをそれぞれのRepositoryに注入（Injection）して、つぎのように利用します。

```cs
public class VendorRepository : IVendorRepository
{
    private readonly PurchasingDatabase _database;

    public VendorRepository(PurchasingDatabase database)
    {
        _database = database;
    }

    public async Task<Vendor> GetVendorByIdAsync(VendorId vendorId)
    {
        using var connection = _database.Open();
```

## ドメインビューの更新

ところでDatabaseパッケージは、どこに実装されるものでしょうか？すでに気が付いている人もいるかもしれませんが、これは認証と同じ位置づけにあります。ということで、ドメインビューまでフィードバックする必要があります。

![](/Article02/スライド30.PNG)

境界付けられたコンテキストに汎用データベースドメインを追加しました。

![](/Article02/スライド31.PNG)

どうようにコンテキストマップに、カスタマー・サプライヤーとして追加しました。

こうやって設計とともにドメインモデルの精度を高めていくことをドメイン駆動設計では蒸留といいます。

## 論理ビューの更新

つづいて論理ビューを更新します。

![](/Article02/スライド29.PNG)

大きな同心円はもともと購買ドメインの実現を表現したものです。データベースドメインは別のドメインで、カスタマー・サプライヤー関係にあります。そのため別の円に切り出しました。

実際はデータベースドメインは必ずしも書く必要はないと思います。というのは、たとえばこの図にDapperはどこに書くのか？それ以前に.NETの標準ライブラリに含まれるintやstringはどこに？ということで、フォーカスしているドメイン外のものを書き始めるときりがなくなるからです。

ただデータベースドメインは現在は書ききれますし、その方が分かりやすいので現時点ではこうしてあります。ごちゃごちゃして書ききれなくなったら、またその時に考えます。

## 実装ビューの作成

論理ビューに少しオブジェクトが登場してきたので、それらをどのようにコンポーネント（プロジェクト）分割するか考えていきましょう。

ということでコンポーネント図を使って実装ビューを作成します。

![](/Article02/スライド32.PNG)

IDatabaseなどはAdventureWorks社のデータベースドメインなので、AdventureWorks.Databaseコンポーネントに配置しました。

VendorなどはAdventureWorks.Purchasingコンポーネントを購買ドメインとして配置しました。

IVendorRepositoryの実装やPurchasingDatabaseはAdventureWorks.Purchasing.SqlServerコンポーネントに配置しました。これは少し議論の余地があるかもしれません。

SqlServerという名称ではなく、たとえばRepositoryのような名前も考えられます。現時点では永続化先は一種類なのでRepositoryとう名前を使っても問題ありません。ただクラウドに置く場合に、業務のトランザクションデータはRDBに、画像のようなバイナリーデータはBLOBサービスに置くといった形で、リポジトリーの保管先が別アーキテクチャになる可能性があります。そのため、具象クラスを実装するコンポーネントの名称には、実装するアーキテクチャに従った名前にしておいた方が良いと考えています。

## 配置ビューの更新

さて、コンポーネントが新しく発生したので配置ビューも更新しましょう。

![](/Article02/スライド33.PNG)

Dapperの利用も決定したので、併せてプロットしています。
# プロセスビューの設計

並行性やパフォーマンス要件で特別な検討が必要と考えられるアーキテクチャを設計します。ほとんどの場合は、.NET（async/awaitなど）やASP.NET Coreなどがになってくれるため、それらを単純に使うだけなら特別な設計は不要です。

今回のケースではWPFのプロセスの起動のみを設計の対象とします。

というのはWPFアプリケーションをGeneric Host上で利用したいためです。

Generic HostはASP.NET CoreなどでWebアプリケーションやWeb APIをホスティングするための仕組みです。Generic Hostでは多用な機能が提供されていますが、とくに重要なのは.NET公式のDependency Injection(DI)コンテナーが含まれている点にあります。そのためモダンなサードバーティライブラリはGeneric Host前提のものが多数ありますし、今後でてくる魅力的なライブラリーもGeneric Hostを対象にリリースされるでしょう。

WPFの標準的な実装ではGeneric Hostにホストされていませんが、できないわけではありません。

## フレームワークの選定

WPFをGeneric Host上にホストするライブラリーは、つぎの2つがNuGet上に公開されています。

1. [Dapplo.Microsoft.Extensions.Hosting.Wpf](https://github.com/dapplo/Dapplo.Microsoft.Extensions.Hosting)
2. [Wpf.Extensions.Hosting](https://github.com/nuitsjp/Wpf.Extensions.Hosting)

今回は後者のWpf.Extensions.Hostingを利用します。

これはWpf.Extensions.Hostingをベースとしている画面遷移フレームワーク「[Kamishibai](https://github.com/nuitsjp/Kamishibai)」を利用したいためです。

Kamishibaiは、つぎのような特徴をもつWPF用の画面遷移フレームワークです。


- Generic Hostのサポート
- MVVMパターンを適用したViewModel起点の画面遷移
- 型安全性の保証された画面遷移時パラメーター
- 画面遷移にともなう一貫性あるイベント通知
- nullableを最大限活用するためのサポート

たとえば画面遷移時に文字列messageを引数として渡したいとします。その場合、遷移先のViewModelをつぎのように実装します。

```cs
[Navigate]
public class FirstViewModel
{
    public FirstViewModel(string message)
    {
        Message = message;
    }

    public string Message { get; }
}
```

すると専用の画面遷移メソッドが自動生成され、つぎのように呼び出すことができます。

```cs
await _presentationService.NavigateToFirstAsync("Hello, KAMISHIBAI!");
```

画面遷移でパラメーターの型不一致が発生したり、デフォルトコンストラクタが前提とならないため、null安全な実装ができる非常に強力な、現状もっとも理想的な画面遷移フレームワーク・・・だと思って私が開発したものです。自画自賛抜きで良くできていると思っているので良かったら使ってみてください。

日本語のドキュメントも十分に容易しています。

- [KAMISHIBAI入門](https://zenn.dev/nuits_jp/books/introduction-to-kamishibai)

## コンテナー初期化とViewの分離

通常WPFプロジェクトを作成すると、アプリケーションのエントリーポイントとXAMLは同じプロジェクト内に作成されます。

しかしこの設計には大きな問題があります。

アプリケーションのエントリーポイントでは、Generic Hostを初期化してアプリケーションをホストします。このときGeneric Hostで利用するDIコンテナーの初期化を行う必要があります。DIコンテナーの初期化を行うという事は、エントリーポイントからはソリューション内のすべてのプロジェクトを参照できる必要があります。

論理ビューを更新してみましょう。

![](/Article02/スライド34.PNG)

Programクラスがアプリケーションのエントリーポイントになります。WPFプロジェクトのcsprojでEnableDefaultApplicationDefinitionをfalseに設定し、Programクラスを自動生成しないように変更し、明示的に作成します。

上図からわかるようにProgramはすべてのレイヤーに対して依存関係が発生してしまいます。

しかしAppやWindowなどからたとえばPurchasingDatabaseを直接触れるようにしておきたくありません。

これを防ぐためには、エントリーポイントとViewのプロジェクトを分離する必要があります。

それは実装ビューで表現します。

## 実装ビューと配置ビューの更新

要素が増えてきたので、レイアウトも論理ビューと近い配置に変更しました。

![](/Article02/スライド35.PNG)

Programを配置したAdventureWorks.Purchasing.Appコンポーネントがすべてのコンポーネントに依存していることが見て取れます。

配置ビューも更新しましょう。

![](/Article02/スライド36.PNG)

購買アプリや購買サービスという曖昧なコンポーネントを削除して、それぞれAdventureWorks.Purchasing.AppとAdventureWorks.Purchasing.Serviceに置き換えて依存関係も整理しました。

クライアントとWeb APIノードの間の依存が一時的に失われましたが、このあとユースケースの実装を設計していくなかですぐに登場してきますので、ここではこのままにしておきましょう。

# さいごに

さて、これでやっとユースケースや非機能要件の実装を設計する準備が整いました。

後編は、つぎのような内容で進めて、アーキテクチャ設計を完成に導きたいと思います。

1. 代表的なユースケースの実現
2. 非機能要件の実現
3. 開発者ビューの設計

という訳で、ここまでお付き合いありがとうございました！後編で再びお会いしましょう！