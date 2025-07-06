/*
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
GitPackageInstallerを生成するウィンドウ

GeneratorWindow.cs
────────────────────────────────────────
バージョン: 1.0.0
2025 Wataame(HWataame)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
*/
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
#pragma warning disable IDE0063

namespace HW.GitPkgInstGen
{
    /// <summary>
    /// GitPackageInstallerを生成するウィンドウ
    /// </summary>
    public class GeneratorWindow : EditorWindow
    {
        /// <summary>
        /// ウィンドウの最小サイズ
        /// </summary>
        private static readonly Vector2 WindowMinSize = new(480, 244);
        /// <summary>
        /// ウィンドウのタイトルのGUI
        /// </summary>
        private static readonly GUIContent TitleContent = new("GitInstallerPackage生成ウィンドウ");
        /// <summary>
        /// 生成ボタンのGUI
        /// </summary>
        private static readonly GUIContent GenerateButtonGUI = new("インストーラーを生成する");

        /// <summary>
        /// パラメーター
        /// </summary>
        private readonly Dictionary<string, string> parameters = new();


        /// <summary>
        /// ウィンドウを表示する
        /// </summary>
        [MenuItem("ツール/Git Package Installer生成ウィンドウ")]
        public static void ShowWindow()
        {
            // ウィンドウを取得する
            var window = GetWindow<GeneratorWindow>();

            // ウィンドウのタイトルを設定する
            window.titleContent = TitleContent;

            // ウィンドウサイズを設定する
            SetWindowSize(window);

            // ウィンドウを表示する
            window.Show();
        }

        /// <summary>
        /// ウィンドウサイズを設定する
        /// </summary>
        /// <param name="window">ウィンドウ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetWindowSize(GeneratorWindow window)
        {
            // スクリーンサイズを取得する
            var screenSize = Screen.currentResolution;

            // ウィンドウサイズを設定する
            var rect = window.position;
            rect.size = WindowMinSize;
            rect.x = screenSize.width / 2 - rect.width / 2;
            rect.y = screenSize.height / 2 - rect.height / 2;
            window.position = rect;

            // 最小サイズを設定する
            window.minSize = WindowMinSize;
            // 最大サイズを設定する(一定以上の差がないと最大サイズの設定が効かないため微小値を加算する)
            window.maxSize = WindowMinSize + Vector2.one * 10e-5f;
        }

        /// <summary>
        /// 有効化時の処理
        /// </summary>
        private void OnEnable()
        {
            // パラメーターをリセットする
            ResetParameters();
        }

        /// <summary>
        /// パラメーターをリセットする
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetParameters()
        {
            parameters.TryAdd(
                InstallerGenerator.TargetPackageNameIdentifier, "com.author.package_name_installer");
            parameters.TryAdd(
                InstallerGenerator.TargetPackageVersionIdentifier, "1.0.0");
            parameters.TryAdd(
                InstallerGenerator.TargetPackageDisplayNameIdentifier, "Package Name Installer");
            parameters.TryAdd(
                InstallerGenerator.TargetPackageAuthorNameIdentifier, "Author");
        }

        /// <summary>
        /// 描画処理
        /// </summary>
        private void OnGUI()
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 88;

            // パラメーターの値の入力画面を描画する
            var contents = InstallerGenerator.Contents;
            for (int i = 0; i < contents.Length; ++i)
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    // パラメーターの値を取得する
                    var (label, paramName) = contents[i];
                    if (!parameters.TryGetValue(paramName, out var parameter)) parameter = "";

                    // パラメーターの値を描画する
                    parameter = EditorGUILayout.TextField(label, parameter);

                    if (change.changed)
                    {
                        // 値が変更された場合
                        if (parameters.ContainsKey(paramName))
                        {
                            // 既に同じパラメーターが存在する場合
                            parameters[paramName] = parameter;
                        }
                        else
                        {
                            // まだ同じパラメーターが存在しない場合
                            parameters.Add(paramName, parameter);
                        }
                    }
                }
            }
            EditorGUIUtility.labelWidth = labelWidth;

            // パラメーターを判定する
            bool isValidParameter = InstallerGenerator.CheckParameters(
                parameters, out var isInvalidPackageName,
                out var isShowPackageNameWarning, out var isInvalidVersion);

            // パッケージを判定する
            bool isDuplicatedPackage = !parameters.TryGetValue(
                InstallerGenerator.TargetPackageNameIdentifier, out var packageName) ||
                !InstallerGenerator.CheckPackage(packageName);

            if (isInvalidPackageName || isShowPackageNameWarning || isInvalidVersion || isDuplicatedPackage)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUILayout.LabelField("エラー一覧");

                    if (isDuplicatedPackage)
                    {
                        // 既に同じ名前のパッケージがプロジェクトに存在する場合
                        if (parameters.TryGetValue(InstallerGenerator.TargetPackageNameIdentifier, out packageName))
                        {
                            // パッケージ名を取得できた場合
                            EditorGUILayout.HelpBox($"パッケージ「{packageName}」は既にプロジェクトに存在します", MessageType.Error);
                        }
                        else
                        {
                            // パッケージ名を取得できなかった場合
                            EditorGUILayout.HelpBox("指定された名称のパッケージは既にプロジェクトに存在します", MessageType.Error);
                        }
                    }

                    if (isInvalidPackageName)
                    {
                        // パッケージ名が不正である場合
                        EditorGUILayout.HelpBox("パッケージ名が正しくありません\n" +
                            "241文字以内で、小文字・ハイフン・アンダースコア・数字(いずれも半角)のみで構成され、" +
                            "「com.作者名.」から開始される必要があります", MessageType.Error);
                    }
                    else if (isShowPackageNameWarning)
                    {
                        // パッケージ名の警告を表示する場合
                        EditorGUILayout.HelpBox("パッケージ名が50文字を超えています\n" +
                            "パッケージ名は50文字以下が推奨されます", MessageType.Info);
                    }

                    if (isInvalidVersion)
                    {
                        // バージョンが不正である場合
                        EditorGUILayout.HelpBox("バージョンが正しくありません\n" +
                            "「major.miner.patch」の形式で、半角数字を使用し、" +
                            "0～999999の範囲でゼロ詰めをしない必要があります", MessageType.Error);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(!isValidParameter || isDuplicatedPackage))
            {
                if (GUILayout.Button(GenerateButtonGUI))
                {
                    // フォーカスを解除する
                    GUI.FocusControl("");

                    // 生成場所をユーザーに要求する
                    string installerRoot = EditorUtility.SaveFolderPanel("", "Packages/", "");
                    if (string.IsNullOrWhiteSpace(installerRoot)) return;

                    // インストーラーを生成する
                    InstallerGenerator.Generate(installerRoot, parameters);
                }
            }

            // 何もないところをクリックした時の処理を行う
            ProcessEmptyClicked();
        }

        /// <summary>
        /// 何もないところをクリックした時の処理を行う
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessEmptyClicked()
        {
            var currentEvent = Event.current;
            if (currentEvent != null && currentEvent.type == EventType.MouseDown)
            {
                // マウスクリックのイベントが最後まで残った場合はフォーカスを解除する
                GUI.FocusControl("");
                Repaint();
            }
        }
    }
}
