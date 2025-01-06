using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MoveController : MonoBehaviour
{
    [Header("UI Elements")]
    public Text pokemonNameText;
    public Transform movesContent;
    public GameObject movePrefab;
    public Image sprite;

    [Header("API Endpoints")]
    private const string API_URL = "https://pokeapi.co/api/v2/pokemon/";

    void Start()
    {
        string pokemonName = PlayerPrefs.GetString("SelectedPokemon");
        if (!string.IsNullOrEmpty(pokemonName))
        {
            StartCoroutine(FetchPokemonMoves(pokemonName));
        }
    }

    IEnumerator FetchPokemonMoves(string pokemonName)
    {
        UnityWebRequest request = UnityWebRequest.Get(API_URL + pokemonName);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var pokemon = JsonUtility.FromJson<PokemonData>(request.downloadHandler.text);
            DisplayMoves(pokemon);
        }
        else
        {
            Debug.LogError("Error fetching Pokemon moves: " + request.error);
        }
    }

    void DisplayMoves(PokemonData pokemon)
    {
        pokemonNameText.text = $"{pokemon.name.FirstCharacterToUpper()} Moves";

        StartCoroutine(PokemonUtility.DisplaySprite(pokemon.sprites.front_default, sprite));

        foreach (Transform child in movesContent) //clears previous moves
        {
            Destroy(child.gameObject);
        }

        foreach (var move in pokemon.moves)
        {
            StartCoroutine(FetchMoveDetails(move.move.url));
        }
    }

    IEnumerator FetchMoveDetails(string moveUrl)
    {
        UnityWebRequest request = UnityWebRequest.Get(moveUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var moveDetail = JsonUtility.FromJson<MoveDetail>(request.downloadHandler.text);
            AddMoveToScrollView(moveDetail);
        }
        else
        {
            Debug.LogError("Error fetching move details: " + request.error);
        }
    }

    void AddMoveToScrollView(MoveDetail moveDetail)
    {
        GameObject moveEntry = Instantiate(movePrefab, movesContent);

        Text moveText = moveEntry.GetComponentInChildren<Text>();

        if (moveText != null)
        {
            moveText.text = $"{moveDetail.name.ToUpper().Replace("-", " ")} " +
                            $" (TYPE: {moveDetail.type.name.ToUpper()})";

            if (typeColors.TryGetValue(moveDetail.type.name.ToLower(), out Color color))
            {
                moveText.color = color;
            }
            else
            {
                moveText.color = Color.white;
            }

        }
        else
        {
            Debug.LogError("Error");
        }
    }

    private readonly Dictionary<string, Color> typeColors = new Dictionary<string, Color>
    {
        { "normal", Color.gray },
        { "fire", new Color(1f, 0.5f, 0f) }, //orange
        { "water", Color.blue },
        { "electric", Color.yellow },
        { "grass", Color.green },
        { "ice", Color.cyan },
        { "fighting", new Color(0.8f, 0.4f, 0.4f) }, //light red
        { "poison", new Color(0.6f, 0.3f, 0.8f) }, //purple
        { "ground", new Color(0.8f, 0.7f, 0.5f) }, //brown
        { "flying", new Color(0.6f, 0.7f, 0.5f) }, //light blue
        { "psychic", new Color(1f, 0.3f, 0.6f) }, //pink
        { "bug", new Color(0.6f, 0.8f, 0.4f) }, //dark green
        { "rock", new Color(0.7f, 0.6f, 0.4f) }, //light brown
        { "ghost", new Color(0.5f, 0.4f, 0.7f) }, //violet
        { "dragon", new Color(0.5f, 0.3f, 0.7f) }, //deep blue
        { "dark", new Color(0.4f, 0.3f, 0.3f) }, //dark grey
        { "steel", new Color(0.7f, 0.7f, 0.8f) }, //light grey
        { "fairy", new Color(1f, 0.6f, 1f) }, //light pink
    };
}

[System.Serializable]
public class MoveWrapper
{
    public MoveDetail move;
}

[System.Serializable]
public class MoveDetail
{
    public string name;
    public TypeWrapper type;
    public string url;
}

