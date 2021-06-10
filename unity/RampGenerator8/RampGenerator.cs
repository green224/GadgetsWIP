using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using Unity.Mathematics;
using static Unity.Mathematics.math;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RampGenerator8 {

/**
 * Rampテクスチャを生成するツール。
 * プレファブでデータを格納するので、Editor拡張にはしない(できない)
 */
sealed class RampGenerator : MonoBehaviour {
	// ------------------------------------- public メンバ --------------------------------------------

	[Serializable] public sealed class OneRamp {
		public string name = "";
		public Gradient[] colorGrad = null;
		public AnimationCurve yMapCurve = null;		//!< Y軸方向のグラデーションの混ぜ具合をどうするかを定義するカーブ
		public int2 size = int2(100,1);
//		[HideInInspector] public Texture2D resultTex = null;
	}

	public OneRamp[] rampList = null;


	// ------------------------------------- private メンバ --------------------------------------------




	// --------------------------------------------------------------------------------------------------
#if UNITY_EDITOR
	/** 設定されているパラメータからRampテクスチャを全再生成する */
	void regenerateTex() {

		var thisPath = AssetDatabase.GetAssetPath(gameObject);
		if (string.IsNullOrEmpty(thisPath)) {
			Debug.LogError("RegenerateはPrefab化してから行ってください");
			return;
		}

		{// 一旦ここでバリデーションをかける
			var names = new HashSet<string>();
			foreach (var i in rampList) {
				if ( names.Contains(i.name) ) {
					Debug.LogError("名前が重複しています："+i.name);
					return;
				}
				if ( i.size.x<1||2048<i.size.x || i.size.y<1||2048<i.size.y ) {
					Debug.LogError("不正なサイズが指定されています："+i.size);
					return;
				}
				if ( i.colorGrad == null ) {
					Debug.LogError("データが未初期化です");
					return;
				}
			}
		}

//		// 現在の生成済みテクスチャを全削除。
//		foreach (var i in rampList) {
//			if (i.resultTex == null) return;
//			DestroyImmediate(i.resultTex);
//		}

		// テクスチャを全生成
		var dirPath = System.IO.Path.GetDirectoryName(thisPath);
		foreach (var i in rampList) {
			var tex = generateTex(i);
	
			var dstPath = dirPath + "/" + i.name + ".png";
			var isOverwrite = System.IO.File.Exists(dstPath);

			System.IO.File.WriteAllBytes(dstPath, tex.EncodeToPNG());
			AssetDatabase.ImportAsset(dstPath);

			if (!isOverwrite) {
				var importer = (TextureImporter)TextureImporter.GetAtPath(dstPath);
				importer.wrapMode = TextureWrapMode.Clamp;
				importer.npotScale = TextureImporterNPOTScale.None;
				importer.mipmapEnabled = false;
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				importer.SaveAndReimport();
			}

//			i.resultTex = AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath);
		}
	}

	/** Ramp用のパラメータ一つ分を使用して、テクスチャを一つ生成する */
	Texture2D generateTex( OneRamp src ) {
		var tex = new Texture2D(src.size.x, src.size.y);

		var cols = new Color[src.size.x * src.size.y];
		for (int y=0; y<src.size.y; ++y)
		for (int x=0; x<src.size.x; ++x) {
			var xRate = (float)x / src.size.x;
			if (src.colorGrad.Length == 1) {
				cols[x + y*src.size.x] = src.colorGrad[0].Evaluate(xRate);
			} else {
				var yRate = clamp(
					src.yMapCurve.Evaluate( (float)y / src.size.y * src.colorGrad.Length ),
					0, src.colorGrad.Length-1
				);
				cols[x + y*src.size.x] = Color.Lerp(
					src.colorGrad[ clamp( (int)yRate, 0, src.colorGrad.Length-1 ) ].Evaluate(xRate),
					src.colorGrad[ clamp( (int)yRate+1, 0, src.colorGrad.Length-1 ) ].Evaluate(xRate),
					yRate - floor(yRate)
				);
			}
		}

		tex.SetPixels(cols);
		return tex;
	}

	[CustomEditor(typeof(RampGenerator))]
	sealed class CustomInspector : Editor {
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();

			EditorGUILayout.Space();
			if (GUILayout.Button("Generate")) {
				((RampGenerator)target).regenerateTex();
			}
		}
	}
#endif
}

}