# Contributing to WordPad Neo

**🚧 現在開発中 / Work in Progress 🚧**

> **⚠️ WordPad Neoは現在開発途中のプロジェクトです。**
>
> 機能や仕様、ソースコードの構成などは、今後の開発に伴って変更される可能性があります。
>
> 開発途中のため、未実装の機能や不具合が存在する場合があります。
>
> コードの変更や新機能の実装、翻訳作業を始める前に、既存のIssueやPull Requestを確認することをおすすめします。

> **⚠️ WordPad Neo is currently under active development.**
>
> Features, specifications, and source code structure may change as development progresses.
>
> Some functionality may be incomplete or contain bugs.
>
> Before starting code contributions, new features, or translations, please review existing Issues and Pull Requests to avoid duplicated work.

---

# 日本語

## はじめに

WordPad Neoへの関心をお寄せいただき、ありがとうございます！

WordPad Neoは、従来のWordPadのシンプルさと使いやすさを維持しながら、現代的なテキストエディタとして発展させることを目指すプロジェクトです。

**現在は開発段階にあり、完成したアプリケーションではありません。**

このプロジェクトでは、開発者だけでなく、翻訳者、デザイナー、テスター、一般ユーザーなど、さまざまな方からの貢献を歓迎しています。

特に、**英語をはじめとする多言語への翻訳協力を積極的に募集しています。**

---

## 1. 貢献できること

WordPad Neoには、さまざまな方法で貢献できます。

### 🌍 翻訳・ローカライズ（特に歓迎！）

WordPad Neoを世界中のユーザーに届けるため、多言語化に協力してくださる方を募集しています。

特に英語への翻訳を歓迎していますが、その他の言語への翻訳も大歓迎です。

翻訳の対象には、以下が含まれます。

- アプリケーションのメニューやボタン
- 設定画面やダイアログ
- エラーメッセージや通知
- READMEなどのドキュメント
- 既存の翻訳の修正や改善

翻訳に参加するために、プログラミングの知識は必須ではありません。

自然でわかりやすい表現への改善も、重要な貢献です。

**開発中のため、翻訳対象となる文字列やファイル構成が今後変更される場合があります。** 作業を始める前に、関連するIssueやPull Requestをご確認ください。

### 🐛 バグ報告

アプリケーションで不具合を発見した場合は、GitHub Issuesから報告してください。

WordPad Neoは現在開発途中のため、不具合や予期しない動作が発生する可能性があります。

報告には、可能な範囲で以下の情報を含めてください。

- 発生した問題の内容
- 問題を再現する手順
- 期待される動作
- 実際に発生した動作
- 使用しているOSやアプリケーションのバージョン
- 必要に応じてスクリーンショット

同じ問題がすでに報告されていないか、Issueを作成する前に確認してください。

### 💡 機能の提案

新しい機能や改善のアイデアも歓迎しています。

WordPad Neoでは、シンプルさと使いやすさを大切にしています。

機能を提案する際は、以下の点を意識してください。

- どのような問題を解決する機能か
- どのようなユーザーに役立つか
- 従来のWordPadの使いやすさを損なわないか
- 現在の開発方針やプロジェクトの目標に合っているか

**大きな変更や新機能の実装を行う場合は、作業を始める前にGitHub Issuesで相談してください。**

開発段階のため、機能の優先順位や実装方針が変更される可能性があります。

### 💻 コードへの貢献

プログラムの改善や新機能の実装も歓迎しています。

基本的な手順は以下のとおりです。

1. リポジトリをForkする
2. 作業用のブランチを作成する
3. 変更を実装する
4. 変更内容を確認し、必要に応じてテストする
5. 変更をCommitしてPushする
6. Pull Requestを作成する

ブランチ名の例：

```text
feature/new-toolbar
fix/document-saving
translation/english
docs/update-readme
```

Pull Requestでは、変更内容とその理由を明確に説明してください。

可能であれば、関連するIssueへのリンクも追加してください。

なお、開発途中のため、コードの構成や実装方法が変更される場合があります。変更を加える前に、最新のソースコードと既存のPull Requestを確認してください。

### 📖 ドキュメントの改善

ドキュメントへの貢献も歓迎しています。

例えば、以下の改善が考えられます。

- 説明文の誤字脱字の修正
- わかりにくい表現の改善
- インストール手順の追記
- 使用方法の説明
- スクリーンショットの追加
- 日本語・英語などの翻訳

現在開発中の機能について記述する場合は、実装済みの機能と開発予定の機能を明確に区別してください。

---

## 2. 翻訳に参加する方法

WordPad Neoでは、翻訳への貢献を特に歓迎しています。

### 翻訳の基本方針

翻訳を行う際は、以下の点を意識してください。

**① 自然でわかりやすい表現**

単語を直訳するのではなく、その言語のユーザーにとって自然な表現を使用してください。

**② 用語の統一**

同じ機能や操作には、できる限り同じ訳語を使用してください。

**③ 元の意味を維持する**

翻訳によって機能の意味や説明内容が変わらないようにしてください。

**④ UIの読みやすさ**

メニューやボタンの文字数が長くなりすぎないよう配慮してください。

**⑤ 特殊な文字列の保持**

翻訳対象に `{filename}`、`%s`、`{0}` などのプレースホルダーが含まれる場合は、原則として変更しないでください。

### 翻訳の提出方法

1. GitHub Issuesで翻訳に関する既存の議論を確認する
2. 翻訳したい言語や対象を選択する
3. 必要に応じてIssueを作成し、作業予定を共有する
4. 該当するファイルを翻訳・修正する
5. Pull Requestを作成する

翻訳ファイルの構成や形式が不明な場合は、GitHub Issuesで質問してください。

新しい言語の追加についても歓迎しています。

**注意：** 現在は開発途中のため、翻訳した文字列が今後の更新で変更・削除される場合があります。大規模な翻訳作業については、事前にIssueで相談することをおすすめします。

---

## 3. Pull Requestのガイドライン

Pull Requestを提出する際は、以下の点にご協力ください。

- 一つのPull Requestでは、できるだけ一つの目的に集中してください。
- 変更内容と理由を明確に記載してください。
- 既存の機能や操作性への影響を確認してください。
- 必要に応じてテストや動作確認を行ってください。
- UIの変更を含む場合は、スクリーンショットを添付してください。
- 他の貢献者の意見やレビューを尊重してください。
- 大きな変更については、事前にIssueで開発方針を相談してください。

WordPad Neoは現在開発段階にあるため、開発方針や実装状況によっては、Pull Requestの修正や変更をお願いする場合があります。

すべてのPull Requestが必ず採用されるわけではありませんが、提案や貢献を歓迎します。

---

## 4. コミュニティでのお願い

WordPad Neoでは、誰もが安心して参加できるコミュニティを目指しています。

他の貢献者に対して敬意を持ち、建設的なコミュニケーションを心がけてください。

技術的な経験や言語能力にかかわらず、互いの貢献を尊重しましょう。

---

## 5. 最後に

WordPad Neoは、コミュニティとともに成長していくプロジェクトです。

**現在はまだ開発途中ですが、世界中の開発者や翻訳者、ユーザーの皆さまとともに、少しずつ完成度を高めていきたいと考えています。**

小さな修正や翻訳の改善も、プロジェクトにとって大切な貢献です。

WordPad Neoの開発に興味を持っていただき、ありがとうございます！

---

# English

## Thank You for Contributing!

WordPad Neo is currently a **work in progress** and is not yet a finished application.

Thank you for your interest in contributing to WordPad Neo!

WordPad Neo aims to bring the classic WordPad experience into the modern era while preserving its simplicity and ease of use.

We welcome contributions from developers, translators, designers, testers, and users of all experience levels.

**We are especially looking for contributors who can help translate WordPad Neo into English and other languages.**

---

## 1. Ways to Contribute

### 🌍 Translation and Localization (Especially Welcome!)

We want WordPad Neo to be accessible to users around the world.

We especially welcome English translations, but contributions in any language are greatly appreciated.

Areas that need translation may include:

- Application menus and buttons
- Settings and dialog boxes
- Error messages and notifications
- Documentation, including the README
- Improvements to existing translations

Programming experience is not required.

Even small improvements to wording, clarity, or consistency can make a meaningful difference.

**Since the project is still under development, translation strings and file structures may change.** Please review existing Issues and Pull Requests before starting translation work.

### 🐛 Reporting Bugs

If you encounter a bug, please report it through GitHub Issues.

Since WordPad Neo is still under development, bugs and unexpected behavior may occur.

When reporting a bug, please include as much of the following information as possible:

- A description of the issue
- Steps to reproduce the problem
- Expected behavior
- Actual behavior
- Operating system and application version
- Screenshots, if applicable

Please check existing Issues before submitting a new report to avoid duplicates.

### 💡 Suggesting Features

We welcome suggestions for new features and improvements.

Please keep in mind that WordPad Neo focuses on simplicity, usability, and preserving the familiar WordPad experience.

When suggesting a feature, consider:

- What problem does it solve?
- Who would benefit from it?
- Does it maintain the simplicity of WordPad?
- Does it align with the project's current goals and development direction?

**For major changes or new features, please open an Issue for discussion before starting implementation.**

Since the project is still evolving, development priorities and implementation plans may change.

### 💻 Code Contributions

Code contributions are welcome!

To contribute code:

1. Fork the repository.
2. Create a new branch.
3. Implement your changes.
4. Test your changes where applicable.
5. Commit and push your changes.
6. Open a Pull Request.

Example branch names:

```text
feature/new-toolbar
fix/document-saving
translation/english
docs/update-readme
```

Please clearly describe your changes and explain why they are needed.

If applicable, link the relevant Issue in your Pull Request.

As the project is still under active development, the codebase and implementation details may change. Please review the latest source code and existing Pull Requests before making changes.

### 📖 Documentation

Documentation improvements are also welcome.

Examples include:

- Fixing typos and grammatical errors
- Improving unclear explanations
- Updating installation instructions
- Adding usage examples
- Adding screenshots
- Translating documentation into other languages

When documenting features under development, please clearly distinguish between implemented features and planned functionality.

---

## 2. Translation Guidelines

Translations are an important part of making WordPad Neo accessible to a global audience.

### General Guidelines

**1. Use Natural Language**

Avoid overly literal translations. Prefer expressions that are clear and natural to native speakers.

**2. Maintain Consistency**

Use consistent terminology for the same features and actions throughout the application.

**3. Preserve the Original Meaning**

Make sure translations accurately reflect the intended meaning of the original text.

**4. Keep UI Text Concise**

Avoid unnecessarily long translations, especially for menus, buttons, and dialog boxes.

**5. Preserve Placeholders**

Do not modify placeholders such as `{filename}`, `%s`, or `{0}` unless explicitly required.

### How to Submit Translations

1. Check existing translation-related GitHub Issues.
2. Choose a language or translation area.
3. Open an Issue if coordination is needed.
4. Translate or improve the relevant files.
5. Submit a Pull Request.

If you are unsure where translation files are located or how they are organized, feel free to ask in GitHub Issues.

Contributions for new languages are welcome!

**Note:** Since WordPad Neo is still under development, translated strings may be changed or removed in future updates. For large translation efforts, we recommend discussing your plans through GitHub Issues first.

---

## 3. Pull Request Guidelines

When submitting a Pull Request, please follow these guidelines:

- Keep each Pull Request focused on a specific change.
- Clearly explain what has changed and why.
- Check whether your changes affect existing functionality.
- Test your changes when applicable.
- Include screenshots for user interface changes when possible.
- Be respectful and constructive during code reviews.
- Discuss major changes through GitHub Issues before implementation.

Since WordPad Neo is under active development, Pull Requests may require revisions to align with the project's evolving architecture and development direction.

Please note that not every Pull Request can be accepted, but all constructive contributions are appreciated.

---

## 4. Community Guidelines

We aim to create a welcoming and respectful community.

Please communicate respectfully, provide constructive feedback, and appreciate contributions from people with different backgrounds and experience levels.

Everyone is welcome to participate.

---

## 5. Final Note

WordPad Neo is a community-driven project.

**Although the project is still in development, we hope to improve it step by step together with developers, translators, and users from around the world.**

Whether you contribute code, translate a few strings, fix a typo, report a bug, or suggest an improvement, your contribution matters.

Thank you for helping make WordPad Neo better for everyone!