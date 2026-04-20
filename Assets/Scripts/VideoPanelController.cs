using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private AspectRatioFitter aspectFitter;
    [SerializeField] private string githubUrl = "https://github.com/DiJeyJr/SafeDriver";
    [SerializeField] private string surveyUrl = "https://docs.google.com/forms/d/e/1FAIpQLScjriPxtxhlOJ9T_fWArpkYAze6tl5-X4s21qa6Zcnt1fEzJA/viewform?usp=publish-editor";

    public void Open()
    {
        if (panel == null || videoPlayer == null) return;

        panel.SetActive(true);
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.Prepare();
    }

    public void Close()
    {
        if (panel == null) return;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.prepareCompleted -= OnPrepared;
        }
        panel.SetActive(false);
    }

    public void OpenGitHub()
    {
        if (!string.IsNullOrEmpty(githubUrl))
            Application.OpenURL(githubUrl);
    }

    public void OpenSurvey()
    {
        if (!string.IsNullOrEmpty(surveyUrl))
            Application.OpenURL(surveyUrl);
    }

    private void OnPrepared(VideoPlayer vp)
    {
        if (videoDisplay != null && vp.renderMode == VideoRenderMode.APIOnly)
            videoDisplay.texture = vp.texture;
        if (aspectFitter != null && vp.height > 0)
            aspectFitter.aspectRatio = (float)vp.width / vp.height;
        vp.Play();
        vp.prepareCompleted -= OnPrepared;
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.prepareCompleted -= OnPrepared;
    }
}
