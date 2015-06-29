﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PanelFramesScript : MonoBehaviour {
	/// <summary>
	/// 共通利用オブジェクト類管理インスタンス（インスペクタで初期化）
	/// </summary>
	public ObjectsController objectsController;
	/// <summary>
	/// frameImgのプレハブ（インスペクタで初期化）
	/// </summary>
	public GameObject frameImgPrefub;
	/// <summary>
	/// モーションデータを管理するインスタンス（インスペクタで初期化）
	/// </summary>
	public GameObject layoutArea;
	/// <summary>
	/// 横方向スクロールバー（インスペクタで初期化）
	/// </summary>
	public Scrollbar scrollBarH;
	/// <summary>
	/// menuコントローラインスタンス（インスペクタで初期化）
	/// Note...menuコントローラからwaitRequestがきてる場合，一切の操作を受け付けないようにする
	/// </summary>
	public MenuGUI menuGUI;
	/// <summary>
	/// フレーム間の隙間サイズ（インスペクタで初期化）
	/// </summary>
	public float framePadding;
	/// <summary>
	/// フレーム削除アニメーション再生中フラグ
	/// Note...このフラグはフレーム（FrameImgScript)でセットされる
	/// </summary>
	public bool isFramePlayingDestroyAnimation = false;
	/// <summary>
	/// frameImgリスト
	/// </summary>
	public List<GameObject> frameImgList = new List<GameObject> ();
	public int FrameCount {
		get { return frameImgList.Count; }
	}
	/// <summary>
	/// 現在選択中のframeImgのindex
	/// </summary>
	private int selectedIndex;
	/// <summary>
	/// フレーム選択判定フラグ
	/// </summary>
	private bool isChildFrameImgClicked = false;
	/// <summary>
	/// 末端スクロール要求フラグ
	/// </summary>
	private bool isRequestToMoveScrollBar = false;
	/// <summary>
	/// 先頭フレームから末端フレームまでの幅
	/// </summary>
	private float layoutAreaWidth;
	private BoxCollider2D layoutAreaCollider;
	private RectTransform thisRectTransform;
	private RectTransform layoutAreaRectTransfrom;
	private int replaceSrcIndex;
	private int replaceDstIndex;

	// Use this for initialization
	void Start () {
		// インスタンス類初期化
		thisRectTransform = this.GetComponent<RectTransform> ();
		layoutAreaRectTransfrom = layoutArea.GetComponent<RectTransform> ();
		layoutAreaCollider = layoutArea.GetComponent<BoxCollider2D> ();
		// 初期フレーム作成
		selectedIndex = 0;
		CreateNewFrameImg (0);

	}
	
	// Update is called once per frame
	void Update () {
		// Frame関連の待機リクエストがあるか判定
		if(objectsController.isFrameRelationWaitRequest == false) {
			// マウスクリックを検知し，フレーム削除中でない，かつアニメーション再生時でないことを確認
			if (Input.GetMouseButtonUp (0) && isFramePlayingDestroyAnimation == false && objectsController.isAnimationPlaying == false) {
				// フレームがクリックされていなく，かつフレーム表示区域をクリック → 新規フレーム作成
				if (isChildFrameImgClicked == false && this.GetComponent<Collider2D>().OverlapPoint(Input.mousePosition)) {
					int createIndex = -1;	// 作成されるフレームのインデックス
					// クリック位置のcollider一覧を取得
					RaycastHit2D[] hits = Physics2D.RaycastAll (Input.mousePosition, -Vector2.up);
					for (int i = 0; i < hits.Length; i++) {
						// クリックされたcolliderの中から先頭フレームから終端フレームまでを覆うcolliderを判別
						// Note...これによりフレームとフレームの隙間をクリックしたことを判定できる
						if (hits [i].collider == layoutAreaCollider) {
							// クリック位置のx座標を取得（クリック座標は画面の座標を基準としているので，基準をフレーム表示区域に）
							//float clickPosX = hits[i].point.x * (thisRectTransform.rect.width / Screen.width);
							float clickPosX = Input.mousePosition.x * (thisRectTransform.rect.width / Screen.width);
							// フレーム表示区域のスクロール量を検知し，スクロール分座標を追加する
							float normalizedPos = this.GetComponent<ScrollRect>().horizontalNormalizedPosition;
							clickPosX += normalizedPos * (layoutAreaWidth - thisRectTransform.rect.width + framePadding);
							// どのフレームの間がクリックされたか判定．フレーム作成位置を決定
							for (int j = 0; j < frameImgList.Count; j++) {

								Debug.Log ("Mouse : " + clickPosX.ToString () +
									"   layout : " + ((j + 1) * layoutAreaWidth / frameImgList.Count).ToString());
								if (clickPosX < (j + 1) * layoutAreaWidth / frameImgList.Count) {
									createIndex = j;
									break;
								}
							}
						}
					}
					// createIndex < 0 → フレームとフレームとの隙間をクリックしていない → フレーム群の末端に新規作成
					if(createIndex < 0){
						createIndex = frameImgList.Count;
						// スクロール位置を末端にするフラグをセット
						isRequestToMoveScrollBar = true;
					}
					// 新規フレーム作成
					CreateNewFrameImg (createIndex);
				} 
				// フレームが選択されていた際はフラグ解除（すでに処理はChildFrameImgClickで終了済み）
				else {
					isChildFrameImgClicked = false;
				}
			}
		}

	}

	void LateUpdate() {
		// FrameImgLayAoutAreaのColliderサイズを調整
		// Note...Content Size FilterによりlayoutAreaのWidthが可変的，かつサイズ変更のタイミングが不明なため，
		//        Colliderのサイズ調整が必要．
		layoutAreaWidth = layoutAreaRectTransfrom.rect.width - framePadding;
		layoutAreaCollider.size = new Vector2 (layoutAreaWidth, layoutAreaCollider.size.y);
		layoutAreaCollider.offset = new Vector2 (layoutAreaWidth / 2, 0f);
	}
	/// <summary>
	/// 全フレームリセット（モーション新規作成）メソッド
	/// </summary>
	public void AllFramesReset() {
		while (frameImgList.Count > 1) {
			ChildFrameImgDestroy (0);
		}
		objectsController.motionData.FrameInitialize (0, true);
		frameImgList [0].GetComponent<FrameImgScript> ().sliderTime.value = objectsController.motionData.frameList [0].transitionTime;
	}
	/// <summary>
	/// 現在選択中のフレームを初期化する（モデルを初期位置にする）メソッド
	/// </summary>
	public void FrameInitialize() {
		objectsController.motionData.FrameInitialize (selectedIndex);
	}
	/// <summary>
	/// フレーム表示区域スクロール検知メソッド（イベント発生）
	/// </summary>
	public void ScrollBarH_OnValueChanged() {
		// 末端までスクロールするフラグがセットされていたら末端まで移動
		if (isRequestToMoveScrollBar == true) {
			scrollBarH.value = 1;
			isRequestToMoveScrollBar = false;
		}

	}

	public bool MotionFramesRead(string motionJsonStr) {
		if (objectsController.motionData.MotionJSONDataRead (motionJsonStr) == false)
			return false;
		foreach (GameObject frameImg in frameImgList) {
			Destroy (frameImg);
		}
		frameImgList.Clear ();

		for (int i = 0; i < objectsController.motionData.frameList.Count; i++) {
			CreateNewFrameImg (i, true);
		}
		StartCoroutine (FrameImgTexInitialize());

		return true;
	}

	/// <summary>
	/// 選択フレームを一つ前にするメソッド
	/// </summary>
	public void FrameGoBack() {
		if (selectedIndex > 0) {
			objectsController.plenAnimation.AnimationPlay (selectedIndex, selectedIndex - 1);
//			CallSelectedFrameImgChanged ();
//			motionData.ChangeSelectFrame (selectedIndex);
		}
	}
	/// <summary>
	/// 選択フレームを一つ次にするメソッド
	/// </summary>
	public void FrameGoNext() {
		if (selectedIndex < frameImgList.Count - 1) {
			objectsController.plenAnimation.AnimationPlay (selectedIndex, selectedIndex + 1);
//			CallSelectedFrameImgChanged ();
//			motionData.ChangeSelectFrame (selectedIndex);
		}
	}
	/// <summary>
	/// フレーム削除メソッド
	/// </summary>
	/// <param name="destroyIndex">削除フレームインデックス</param>
	public void ChildFrameImgDestroy(int destroyIndex) {
		// フレーム選択フラグセット
		isChildFrameImgClicked = true;
		isFramePlayingDestroyAnimation = false;
		// すでにフレームが一つしかない場合は削除できない
		if (frameImgList.Count == 1)
			return;
		// フレームオブジェクト破棄．リストから除去．
		Destroy (frameImgList [destroyIndex]);
		frameImgList.RemoveAt (destroyIndex);
		objectsController.motionData.FrameRemove (destroyIndex);
		// 各フレームのインデックスを再設定（影響のあるフレームのみ）
		for (int i = destroyIndex; i < frameImgList.Count; i++) {
			frameImgList [i].GetComponent<FrameImgScript> ().index = i;
		}
		// フレームと関節情報管理インスタンスとの関連付けを行う (フレームが
		for (int i = 0; i < frameImgList.Count; i++) {
//			frameImgList [i].GetComponent<FrameImgScript> ().thisFrame = motionData.frameList [i];
		}

		if (selectedIndex > frameImgList.Count - 1) {
			selectedIndex = frameImgList.Count - 1;
			CallSelectedFrameImgChanged ();
			objectsController.motionData.ChangeSelectFrame (selectedIndex);
		}
		else if (selectedIndex == destroyIndex) {
			CallSelectedFrameImgChanged ();
			objectsController.motionData.ChangeSelectFrame (selectedIndex);
		}
	}

	public void ChildFrameImgClick(int frameIndex) {
		selectedIndex = frameIndex;
		isChildFrameImgClicked = true;
		CallSelectedFrameImgChanged ();
		objectsController.motionData.ChangeSelectFrame (selectedIndex);
	}

	public void PlayAnimationEnded(int endClipIndex) {
		selectedIndex = endClipIndex;
		CallSelectedFrameImgChanged (false);
		objectsController.motionData.ChangeSelectFrame (selectedIndex);
	}

	public void AnimationStarted(int startClipIndex) {
		frameImgList [selectedIndex].GetComponent<FrameImgScript> ().SaveFrameImgTex ();
		selectedIndex = startClipIndex;
		CallSelectedFrameImgChanged (true);
	}

	public void AnimationClipChanged(int changeClipIndex) {
		selectedIndex = changeClipIndex;
		CallSelectedFrameImgChanged (true);
	}

	public void FrameImgReplace(int srcIndex, int dstIndex) {
		selectedIndex = srcIndex;

		FrameImgScript srcFrameImg = frameImgList [srcIndex].GetComponent<FrameImgScript> ();
		FrameImgScript dstFrameImg = frameImgList [dstIndex].GetComponent<FrameImgScript>();
		//Frame srcFrame = objectsController.motionData.frameList [srcIndex].Clone ();
		Frame srcFrame = new Frame (objectsController.motionData.frameList [srcIndex]);

		objectsController.motionData.frameList [srcIndex] = objectsController.motionData.frameList [dstIndex];
		objectsController.motionData.frameList [dstIndex] = srcFrame;

		dstFrameImg.index = dstIndex;
		srcFrameImg.index = srcIndex;
		dstFrameImg.thisFrame = objectsController.motionData.frameList [dstIndex];
		srcFrameImg.thisFrame = objectsController.motionData.frameList [srcIndex];
		dstFrameImg.sliderTime.value = dstFrameImg.thisFrame.transitionTime;
		srcFrameImg.sliderTime.value = srcFrameImg.thisFrame.transitionTime;

		selectedIndex = dstIndex;
		CallSelectedFrameImgChanged (false);
		objectsController.motionData.ChangeSelectFrame (selectedIndex);
	}

	private void CallSelectedFrameImgChanged(bool isAnimating = false) {
		foreach (GameObject childFrameImg in frameImgList) {
			childFrameImg.GetComponent<FrameImgScript> ().SelectedFrameImgChanged (selectedIndex, isAnimating);
		}
	}
	private void CreateNewFrameImg(int createIndex, bool isFrameDataExist = false) {

		frameImgList.Insert (createIndex, GameObject.Instantiate (frameImgPrefub));

		// 各フレームのインデックスを再設定（影響のあるフレームのみ）
		for (int i = createIndex; i < frameImgList.Count; i++)
			frameImgList [i].GetComponent<FrameImgScript> ().index = i;

		layoutArea.transform.DetachChildren ();
		foreach (GameObject frameImg in frameImgList) {
			frameImg.transform.SetParent (layoutArea.transform, false);
			frameImg.transform.localScale = new Vector3 (1f, 1f, 1f);
		}
		if (isFrameDataExist == false)
			objectsController.motionData.CreateNewFrame (createIndex, selectedIndex);
		else
			objectsController.motionData.ChangeSelectFrame (createIndex);

		for (int i = 0; i < frameImgList.Count; i++)
			frameImgList [i].GetComponent<FrameImgScript> ().thisFrame = objectsController.motionData.frameList [i];

		FrameImgScript newFrameImg = frameImgList [createIndex].GetComponent<FrameImgScript> ();
		newFrameImg.sliderTime.value = newFrameImg.thisFrame.transitionTime;

		frameImgList [createIndex].SetActive (true);

		selectedIndex = createIndex;
		CallSelectedFrameImgChanged ();
	}

	private IEnumerator FrameImgTexInitialize() {
		for (int i = 1; i < frameImgList.Count; i++) {
			selectedIndex = i;
			objectsController.motionData.ChangeSelectFrame (i);
			CallSelectedFrameImgChanged (false);
			yield return null;
		}
		selectedIndex = 0;
		objectsController.motionData.ChangeSelectFrame (0);
		CallSelectedFrameImgChanged (false);
	}
}
