/*
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
GitPackageInstallerの生成処理を保持するクラス

InstallerGenerator.cs
────────────────────────────────────────
バージョン: 1.0.0
2025 Wataame(HWataame)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#pragma warning disable IDE0063

namespace HW.GitPkgInstGen
{
    /// <summary>
    /// GitPackageInstallerの生成処理を保持するクラス
    /// </summary>
    public static class InstallerGenerator
    {
        /// <summary>
        /// パッケージ名の最大文字数
        /// </summary>
        private const int MaxPackageNameLength = 214;
        /// <summary>
        /// パッケージ名の警告を出さない最大文字数
        /// </summary>
        private const int MaxNonWarningPackageNameLength = 50;
        /// <summary>
        /// 作業フォルダーの親フォルダーのパスのベース文字列
        /// </summary>
        private const string WorkFolderPathBaseParent = "Assets";
        /// <summary>
        /// 作業フォルダーのフォルダー名のベース文字列
        /// </summary>
        private const string WorkFolderNameBase = "GPIGTemp";
        /// <summary>
        /// RepositoryTableのソースコードアセットのGUIDのパラメーター名
        /// </summary>
        private const string RepositoryTableGuidParamName = "RepoTblSrcGuid";
        /// <summary>
        /// RepositoryTableのソースコードアセットの相対パス
        /// </summary>
        private const string RepositoryTableSourceGuidFilePath = "Core/Data/GitRepositoryTable.cs";
        /// <summary>
        /// 空のGUIDの文字列
        /// </summary>
        private const string EmptyGUIDText = "00000000000000000000000000000000";
        /// <summary>
        /// 処理対象のパッケージ名の識別子
        /// </summary>
        internal const string TargetPackageNameIdentifier = "PkgName";
        /// <summary>
        /// 処理対象のパッケージのバージョンの識別子
        /// </summary>
        internal const string TargetPackageVersionIdentifier = "PkgVer";
        /// <summary>
        /// 処理対象のパッケージ表示名の識別子
        /// </summary>
        internal const string TargetPackageDisplayNameIdentifier = "PkgDisplayName";
        /// <summary>
        /// 処理対象のパッケージの作者名の識別子
        /// </summary>
        internal const string TargetPackageAuthorNameIdentifier = "PkgAuthor";
        /// <summary>
        /// 名前空間のマーカー文字列
        /// </summary>
        internal const string NameSpaceMarker = @"\!<NameSpace>";

        /// <summary>
        /// パラメーターと表示の組み合わせの配列
        /// </summary>
        private static readonly (GUIContent label, string paramName)[] contents = new (GUIContent, string)[]
        {
            (new("パッケージ名", "生成するパッケージのパッケージ名(com.xxx.yyy形式)"), TargetPackageNameIdentifier),
            (new("バージョン", "生成するパッケージのバージョン(major.miner.patch形式)"), TargetPackageVersionIdentifier),
            (new("パッケージ表示名", "生成するパッケージのPackageManagerやProjectViewでの表示名"), TargetPackageDisplayNameIdentifier),
            (new("パッケージ作者名", "生成するパッケージの作者名"), TargetPackageAuthorNameIdentifier),
        };

        /// <summary>
        /// パラメーターと表示の組み合わせの配列
        /// </summary>
        public static ReadOnlySpan<(GUIContent label, string paramName)> Contents
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => contents;
        }


        /// <summary>
        /// パラメーターを判定する
        /// </summary>
        /// <param name="parameters">パラメーター</param>
        /// <param name="isInvalidPackageName">パッケージ名が正しくないか</param>
        /// <param name="isShowPackageNameWarning">パッケージ名の警告を表示するか</param>
        /// <param name="isInvalidVersion">バージョンが正しくないか</param>
        /// <returns>判定結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckParameters(Dictionary<string, string> parameters,
            out bool isInvalidPackageName, out bool isShowPackageNameWarning, out bool isInvalidVersion)
        {
            // 参照渡しの引数を初期化する
            isInvalidPackageName = true;
            isShowPackageNameWarning = true;

            // パッケージ名を検証する
            if (parameters.TryGetValue(TargetPackageNameIdentifier, out var packageName) &&
                !string.IsNullOrWhiteSpace(packageName) && packageName.Length <= MaxPackageNameLength &&
                Regex.IsMatch(packageName, @"^(com.[a-z0-9][a-z0-9\-_]*?\.[a-z0-9][a-z0-9\-_]*?(\.[a-z0-9\-_]+)*?)$"))
            {
                // パッケージ名の形式が正しい場合
                isInvalidPackageName = false;
                isShowPackageNameWarning = packageName.Length > MaxNonWarningPackageNameLength;
            }

            // バージョンを検証する(major.miner.patch形式、それぞれ0～999999の範囲)
            isInvalidVersion = !parameters.TryGetValue(TargetPackageVersionIdentifier, out var version) ||
                string.IsNullOrWhiteSpace(version) ||
                !Regex.IsMatch(version, @"^((0|[1-9]\d{0,5})\.(0|[1-9]\d{0,5})\.(0|[1-9]\d{0,5}))$");

            return !isInvalidPackageName && !isInvalidVersion;
        }

        /// <summary>
        /// パッケージを判定する
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckPackage(string packageName)
        {
            // 引数が有効ではない場合は失敗
            if (string.IsNullOrWhiteSpace(packageName)) return false;

            // 指定されたパッケージのpackage.jsonが存在しない場合はそのパッケージが存在しないと判定する
            return !AssetDatabase.LoadAssetAtPath<Object>("Packages/" + packageName + "/package.json");
        }

        /// <summary>
        /// インストーラーを生成する
        /// </summary>
        /// <param name="installerRoot">生成場所のルートパス</param>
        /// <param name="parameters">パラメーター</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Generate(string installerRoot, Dictionary<string, string> parameters)
        {
            if (!GetNameSpaceFromPackageName(parameters, out var nameSpace))
            {
                // パッケージ名から名前空間を取得できなかった場合
                Debug.LogError($"[Git Package Installer] パッケージ名が正しくありません");
                EditorUtility.DisplayDialog("Git Package Installer",
                    $"パッケージ名が正しくありません\n処理を終了します", "確認");
                return;
            }

            if (!CheckInstallerRoot(installerRoot))
            {
                // インストーラーのルートディレクトリの判定の結果、処理が許可されなかった場合
                Debug.LogError($"[Git Package Installer] 「{installerRoot}」内が空ではありません");
                EditorUtility.DisplayDialog("Git Package Installer",
                    $"「{installerRoot}」内が空ではありません\n処理を終了します", "確認");
                return;
            }

            // このソースコードはパッケージのルートから1階層下のディレクトリに存在する
            // (root/Source/GeneratorWindow.cs)ため、2回親ディレクトリを取得する
            var packageRootPath = GetParentPath(GetParentPath(GetSelfPath())).Replace('\\', '/');

            // 自身のパッケージ名とテンプレートのルートディレクトリのパスを取得する
            var selfPackageName = GetSelfPackageName(packageRootPath);
            string templateRootPath = $"Packages/{selfPackageName}/Templates";

            // 作業フォルダーを生成する
            GenerateWorkFolder(out var workFolderPath);
            try
            {
                // アセンブリのリロードをロックする
                EditorApplication.LockReloadAssemblies();

                // ファイルを生成する
                var preAssets = new HashSet<string>();
                GenerateFiles(workFolderPath, templateRootPath, parameters, nameSpace, preAssets);

                // アセットデータベースをリフレッシュする
                AssetDatabase.Refresh();

                // テーブルのソースコード(のアセット)のGUIDを取得する
                var tableSrcGuid = GetGuid(workFolderPath + "/" + RepositoryTableSourceGuidFilePath, parameters);

                // テーブルのGUIDのマーカーを置き換える
                ReplaceTableGuid(workFolderPath, tableSrcGuid, preAssets);

                // ファイルを移動する
                MoveFiles(workFolderPath, installerRoot);
            }
            finally
            {
                // 作業フォルダーを削除する
                Directory.Delete(workFolderPath, true);
                File.Delete(workFolderPath + ".meta");

                // アセットデータベースをリフレッシュする
                AssetDatabase.Refresh();

                // アセンブリのリロードのロックを解除する
                EditorApplication.UnlockReloadAssemblies();
            }

            // アセンブリのリロードを要求する
            EditorUtility.RequestScriptReload();
        }

        /// <summary>
        /// パッケージ名から名前空間を取得する
        /// </summary>
        /// <param name="parameters">パラメーター</param>
        /// <param name="nameSpace">名前空間</param>
        /// <returns>処理結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetNameSpaceFromPackageName(Dictionary<string, string> parameters, out string nameSpace)
        {
            if (parameters == null ||
                !parameters.TryGetValue(TargetPackageNameIdentifier, out var packageName) ||
                string.IsNullOrWhiteSpace(packageName))
            {
                // パッケージ名を取得できなかった場合は失敗
                nameSpace = null;
                return false;
            }

            bool isBeforeSeparator = true;
            List<char> nameSpaceBuilder = new(packageName.Length);
            for (int i = 0; i < packageName.Length; ++i)
            {
                char currentChar = packageName[i];
                if (char.IsLower(currentChar))
                {
                    if (isBeforeSeparator)
                    {
                        // 前の文字が小文字以外であった場合
                        nameSpaceBuilder.Add(char.ToUpper(currentChar));
                        isBeforeSeparator = false;
                    }
                    else
                    {
                        // 前の文字が小文字であった場合
                        nameSpaceBuilder.Add(currentChar);
                    }
                }
                else
                {
                    // 現在の文字が小文字以外である場合
                    isBeforeSeparator = true;
                    if (char.IsDigit(currentChar))
                    {
                        // 現在の文字が数字である場合
                        nameSpaceBuilder.Add(currentChar);
                    }
                }
            }

            // 取得した文字から文字列を生成する
            nameSpace = new(nameSpaceBuilder.ToArray());
            return true;
        }

        /// <summary>
        /// インストーラーのルートディレクトリを検証する
        /// </summary>
        /// <param name="installerRoot">インストーラーのルートディレクトリ</param>
        /// <returns>判定結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckInstallerRoot(string installerRoot)
        {
            var root = new DirectoryInfo(installerRoot);

            // ルート直下にディレクトリが存在する場合は処理を許可しない
            ReadOnlySpan<DirectoryInfo> directories = root.GetDirectories();
            if (directories.Length > 0) return false;

            // ルート直下にファイルが存在しない場合は処理を許可する
            ReadOnlySpan<FileInfo> files = root.GetFiles();
            return files.Length == 0;
        }

        /// <summary>
        /// 自身のパスを取得する
        /// </summary>
        /// <param name="doNotAssign">コンパイル時に呼び出し元ソースのパスが入る</param>
        /// <returns>自身のパス</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetSelfPath([CallerFilePath] string doNotAssign = null)
        {
            return doNotAssign;
        }

        /// <summary>
        /// 親ディレクトリのパスを取得する
        /// </summary>
        /// <param name="path">パス</param>
        /// <returns>親ディレクトリのパス</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetParentPath(string path)
        {
            ReadOnlySpan<char> pathSpan = path;
            for (int i = pathSpan.Length - 1; i >= 0; --i)
            {
                if (pathSpan[i] == '/' || pathSpan[i] == '\\')
                {
                    // パスの区切り文字の前までの文字列を返す
                    return path[..i];
                }
            }

            // パスの区切り文字が存在しない場合は入力文字列をそのまま返す
            return path;
        }

        /// <summary>
        /// 自身のパッケージ名を取得する
        /// </summary>
        /// <param name="packageRootPath">パッケージのルートパス</param>
        /// <returns>パッケージ名</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetSelfPackageName(string packageRootPath)
        {
            var packageJsonPath = Path.GetFullPath(packageRootPath + "/package.json");
            if (!File.Exists(packageJsonPath))
            {
                // package.jsonが存在しない場合は失敗
                return null;
            }

            using (var fs = new FileStream(packageJsonPath, FileMode.Open, FileAccess.Read))
            {
                // package.jsonの内容を読み込む
                byte[] data = new byte[(int)fs.Length];
                fs.Read(data, 0, data.Length);

                // package.jsonを解釈する
                if (PackageJsonInformation.GetPackageNameFrom(
                    Encoding.UTF8.GetString(data), out var packageName))
                {
                    // package.jsonを解釈できた場合はnameの値を返す
                    return packageName;
                }
                else
                {
                    // package.jsonを解釈できなかった場合はnullを返す
                    return null;
                }
            }
        }

        /// <summary>
        /// 作業フォルダーを生成する
        /// </summary>
        /// <param name="folderPath">フォルダーの相対パス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateWorkFolder(out string folderPath)
        {
            string path = WorkFolderPathBaseParent;
            string folderName = WorkFolderNameBase;
            for (int i = 0; ; ++i)
            {
                // フォルダーのパスを構築する
                folderPath = path + "/" + folderName;

                if (!Directory.Exists(folderPath))
                {
                    // フォルダーが存在しない場合はそこにフォルダーを生成する
                    AssetDatabase.CreateFolder(path, folderName);
                    return;
                }

                // フォルダーが存在した場合は文字列の末尾のGUIDを付与する
                folderName = WorkFolderNameBase + Guid.NewGuid().ToString("N");
            }
        }

        /// <summary>
        /// テンプレートからファイルを生成する
        /// </summary>
        /// <param name="destination">生成先</param>
        /// <param name="fileRootPath">テンプレートファイルのルートディレクトリ</param>
        /// <param name="parameters">パラメーター</param>
        /// <param name="nameSpace">名前空間</param>
        /// <param name="preAssets">生成されたアセット</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateFiles(string destination, string fileRootPath,
            Dictionary<string, string> parameters, string nameSpace, HashSet<string> preAssets)
        {
            // ファイルを列挙する
            var paths = AssetDatabase.FindAssets("", new[] { fileRootPath })
                .Select(x => AssetDatabase.GUIDToAssetPath(x));
            foreach (var path in paths)
            {
                // 既に生成されたファイルのパスが出現した場合は次のファイルの処理に進む
                if (preAssets.Contains(path)) continue;

                // 相対パスを取得する
                string relativePath = Path.GetRelativePath(fileRootPath, path).Replace('\\', '/');

                string assetPath = destination + "/" + relativePath;

                if (AssetDatabase.IsValidFolder(path))
                {
                    // アセットがフォルダーである場合
                    if (!Directory.Exists(assetPath))
                    {
                        // フォルダーを生成する
                        int index = assetPath.LastIndexOf('/');
                        AssetDatabase.CreateFolder(assetPath[..index], assetPath[(index + 1)..]);
                    }
                }
                else if (path.ToLower().EndsWith(".txt"))
                {
                    // ファイルの拡張子がtxtである場合
                    var sourceAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (sourceAsset)
                    {
                        // アセットがテキストアセットである場合
                        // パスの末尾のtxtを除去して生成先に保存する
                        string afterPath = assetPath[..^4];
                        using (var fs = new FileStream(afterPath, FileMode.Create, FileAccess.Write))
                        {
                            // 書き込む内容を準備する
                            string packageText = ReplaceUserParameters(sourceAsset.text, parameters);

                            // ファイルに内容を書き込む
                            fs.Write(Encoding.UTF8.GetBytes(packageText.Replace(NameSpaceMarker, nameSpace)));
                        }

                        // 生成したファイルをアセットとしてインポートする
                        AssetDatabase.ImportAsset(afterPath, ImportAssetOptions.ForceUpdate);

                        // 事前アセットの連想配列に追加する
                        preAssets.Add(relativePath[..^4]);
                    }
                }
            }
        }

        /// <summary>
        /// GUIDを取得する
        /// </summary>
        /// <param name="assetPath">アセットのパス</param>
        /// <param name="parameters">パラメーター</param>
        /// <returns>GUID</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetGuid(string assetPath, Dictionary<string, string> parameters)
        {
            string metaPath = assetPath + ".meta";
            if (File.Exists(metaPath))
            {
                string inputText;
                using (var fs = new FileStream(metaPath, FileMode.Open, FileAccess.Read))
                {
                    // .metaファイルを読み込む
                    byte[] data = new byte[fs.Length];
                    fs.Read(data);

                    // 文字列を取得する
                    inputText = Encoding.UTF8.GetString(data);
                }

                // .metaファイルからGUIDを取得する
                var match = Regex.Match(inputText, @"^(fileFormatVersion: \d{1,})\nguid: (?<guid>[0-9a-fA-F]{32})");
                try
                {
                    // GUIDを取得して返す
                    if (match.Success)
                    {
                        string guid = match.Groups["guid"].Value;
                        if (guid != EmptyGUIDText) return guid;
                    }
                }
                catch (Exception) { }
            }

            // GUIDを取得できなかった場合はアセットデータベースから取得する
            return AssetDatabase.GUIDFromAssetPath(
                $"Packages/{parameters[TargetPackageNameIdentifier]}/{RepositoryTableSourceGuidFilePath}").ToString();
        }

        /// <summary>
        /// 文字列マーカーをテーブルのGUIDで置き換える
        /// </summary>
        /// <param name="folderPath">生成先</param>
        /// <param name="guid">GUID</param>
        /// <param name="preAssets">生成されたアセット</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReplaceTableGuid(string folderPath, string guid, HashSet<string> preAssets)
        {
            foreach (var path in preAssets)
            {
                string text;
                string assetPath = folderPath + "/" + path;
                using (var fs = new FileStream(assetPath, FileMode.Open, FileAccess.Read))
                {
                    // ファイルの内容を読み込む
                    byte[] data = new byte[fs.Length];
                    fs.Read(data);

                    // ファイルの内容を文字列として取得する
                    text = Encoding.UTF8.GetString(data);
                }

                using (var fs = new FileStream(assetPath, FileMode.Create, FileAccess.Write))
                {
                    // テーブルのGUIDを書き換えて保存し直す
                    fs.Write(Encoding.UTF8.GetBytes(ReplaceGuidParameters(text, guid)));
                }
            }
        }

        /// <summary>
        /// マーカー文字列をユーザー設定パラメーターに置き換える
        /// </summary>
        /// <param name="input">入力文字列</param>
        /// <param name="parameters">パラメーター</param>
        /// <returns>置き換え後の文字列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ReplaceUserParameters(string input, Dictionary<string, string> parameters)
        {
            string output = input;

            // パラメーター内にパラメーターのマーカー文字列があっても変換しないように仮文字列に変換する
            ReadOnlySpan<(GUIContent label, string paramName)> contents = Contents;
            for (int i = 0; i < contents.Length; ++i)
            {
                output = output.Replace(@$"\!<{contents[i].paramName}>", $"\0{(char)i}\0");
            }

            // 仮文字列をパラメーターの値に変換する
            for (int i = 0; i < contents.Length; ++i)
            {
                output = output.Replace($"\0{(char)i}\0", parameters[contents[i].paramName]);
            }

            return output;
        }

        /// <summary>
        /// マーカー文字列をテーブルのソースコードのGUIDに置き換える
        /// </summary>
        /// <param name="input">入力文字列</param>
        /// <returns>置き換え後の文字列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ReplaceGuidParameters(string input, string guid)
        {
            string output;

            // マーカー文字列をテーブルのソースコードのGUIDに置き換える
            output = input.Replace(@"\!<" + RepositoryTableGuidParamName + ">", guid);

            return output;
        }

        /// <summary>
        /// ファイルを移動する
        /// </summary>
        /// <param name="fileRoot">移動元</param>
        /// <param name="installerRoot">移動先</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MoveFiles(string fileRoot, string installerRoot)
        {
            ReadOnlySpan<string> directories = Directory.GetDirectories(fileRoot);
            for (int i = 0; i < directories.Length; ++i)
            {
                // ルートディレクトリ直下のディレクトリを移動する
                string relativePath = Path.GetRelativePath(fileRoot, directories[i]);
                Directory.Move(fileRoot + "/" + relativePath, installerRoot + "/" + relativePath);
            }

            ReadOnlySpan<string> files = Directory.GetFiles(fileRoot);
            for (int i = 0; i < files.Length; ++i)
            {
                // ルートディレクトリ直下のファイルを移動する
                string relativePath = Path.GetRelativePath(fileRoot, files[i]);
                File.Move(fileRoot + "/" + relativePath, installerRoot + "/" + relativePath);
            }

            // アセットデータベースをリフレッシュする
            AssetDatabase.Refresh();
        }
    }
}
