using UnityEngine;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Clips")]
    public AudioClip selectClip;
    public AudioClip matchClip;
    public AudioClip rowClearClip;
    public AudioClip addNumberClip;
    private AudioSource _source;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();
    }


    public void PlaySelect()    => Play(selectClip);
    private float _lastMatchTime = 0f;
    public void PlayMatch()
    {
        if (Time.time - _lastMatchTime < 0.1f) return;
        _lastMatchTime = Time.time;
        Play(matchClip);
    }
    public void PlayRowClear()  => Play(rowClearClip);
    public void PlayAddNumber() => Play(addNumberClip);


    private void Play(AudioClip clip)
    {
        if (clip == null || _source == null) return;
        _source.PlayOneShot(clip);
    }
}
