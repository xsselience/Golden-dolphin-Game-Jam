using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class music : MonoBehaviour
{
    [SerializeField] private AudioClip introClip;   // 前10秒
    [SerializeField] private AudioClip loopClip;    // 第11-60秒
    [SerializeField] private AudioSource source;

    private void Start()
    {
        source = GetComponent<AudioSource>();
        // 播放前奏，设置循环段在结束时自动播放
        source.clip = introClip;
        source.loop = false;
        source.Play();

        // 用协程在前奏结束后切换到循环段
        StartCoroutine(PlayLoopAfterIntro());
    }

    private System.Collections.IEnumerator PlayLoopAfterIntro()
    {
        yield return new WaitForSeconds(introClip.length);

        source.clip = loopClip;
        source.loop = true;
        source.Play();
    }
}
