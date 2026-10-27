using UnityEngine;

public class ActionFeedback : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failureSound;
    [SerializeField] private TextMesh message;
    [SerializeField] private float displaySeconds = 3f;
    [SerializeField] private bool faceViewer = true;
    private float hideAt;
    private Camera viewer;

    private void Start() => viewer = Camera.main;

    public void Success(string text) => Show(text, successSound);
    public void Failure(string text) => Show(text, failureSound);

    private void Show(string text, AudioClip sound)
    {
        message.text = FarmProgression.Wrap(text, 48);
        message.gameObject.SetActive(true);
        hideAt = Time.time + displaySeconds;
        audioSource.PlayOneShot(sound);
    }

    private void Update()
    {
        if (message.gameObject.activeSelf && Time.time >= hideAt)
            message.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (faceViewer && message.gameObject.activeSelf && viewer != null)
            message.transform.rotation = Quaternion.LookRotation(message.transform.position - viewer.transform.position);
    }
}
