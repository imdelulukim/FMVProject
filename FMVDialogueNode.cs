using System;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "FMV/Dialogue Node", fileName = "FMVDialogueNode")]
public class FMVDialogueNode : ScriptableObject
{
    [Header("영상")]
    public VideoClip video;
    
    [Header("대사")]
    [TextArea]
    public string text;

    [Header("설명")]
    [TextArea]
    public string description;

    [Header("선택지 O")]
    public Choice[] choices;

    [Header("선택지 X")]
    public FMVDialogueNode nextNode;

    [Header("엔딩 여부")]
    public bool isEnding;

    [Serializable]
    public class Choice
    {
        public string choiceText;
        public FMVDialogueNode nextNode;
        public bool enabled = true;

        [Header("호감도")]
        public int affection;
    }
}