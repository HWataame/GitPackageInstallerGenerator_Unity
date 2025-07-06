/*
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
package.jsonから必要な情報を取得するためのクラス

PackageJsonInformation.cs
────────────────────────────────────────
バージョン: 1.0.0
2025 Wataame(HWataame)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
*/
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace HW.GitPkgInstGen
{
    /// <summary>
    /// package.jsonから必要な情報を取得するためのクラス
    /// </summary>
    [Serializable]
    internal class PackageJsonInformation
    {
        /// <summary>
        /// パッケージ名
        /// </summary>
        [SerializeField]
        private string name;


        /// <summary>
        /// パッケージ名を取得する
        /// </summary>
        /// <param name="inputJson">入力のJSONの文字列</param>
        /// <param name="packageName">パッケージ名</param>
        /// <returns>処理結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetPackageNameFrom(string inputJson, out string packageName)
        {
            try
            {
                // JSONから値を取得する
                packageName = JsonUtility.FromJson<PackageJsonInformation>(inputJson)?.name;
                return true;
            }
            catch (Exception)
            {
                // 例外が発生した場合
                packageName = null;
                return false;
            }
        }
    }
}
