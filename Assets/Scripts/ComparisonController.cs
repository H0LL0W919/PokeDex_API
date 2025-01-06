using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ComparisonController : MonoBehaviour
{
    [Header("UI Elements")]
    public InputField pokemon1Input;
    public InputField pokemon2Input;
    public Button compareButton;
    public Text pokemon1StatText;
    public Text pokemon2StatText;
    public Dropdown dropDown1;
    public Dropdown dropDown2;
    public Image pokemon1Sprite;
    public Image pokemon2Sprite;
    public Slider[] pokemon1StatBars;
    public Slider[] pokemon2StatBars;

    [Header("API Endpoint")]
    private const string API_URL = "https://pokeapi.co/api/v2/pokemon/";

    [Header("Pokemon Name List")]
    private List<string> pokemonNames = new List<string>();

    private void Start()
    {
        compareButton.onClick.AddListener(ComparePokemon);

        //fetching all pokemon names
        StartCoroutine(PokemonUtility.GetAllPokémonNames(names => pokemonNames = names));

        //input listeners for autocomplete
        pokemon1Input.onValueChanged.AddListener(input => PokemonUtility.DisplayNameSuggestions(input, pokemonNames, dropDown1));
        pokemon2Input.onValueChanged.AddListener(input => PokemonUtility.DisplayNameSuggestions(input, pokemonNames, dropDown2));

        // listeners for dropdown selection
        dropDown1.onValueChanged.AddListener(index => PokemonUtility.SetPokémonName(index, dropDown1, pokemon1Input));
        dropDown2.onValueChanged.AddListener(index => PokemonUtility.SetPokémonName(index, dropDown2 , pokemon2Input));
    }

    void ComparePokemon()
    {
        string pokemon1Name = pokemon1Input.text.ToLower();
        string pokemon2Name = pokemon2Input.text.ToLower();

        if (!string.IsNullOrEmpty(pokemon1Name) && !string.IsNullOrEmpty(pokemon2Name))
        {
            StartCoroutine(FetchPokemonData(pokemon1Name, 1));
            StartCoroutine(FetchPokemonData(pokemon2Name, 2));
        }
        else
        {
            Debug.LogError("Both Pokemon names must be entered!");
        }
    }

    IEnumerator FetchPokemonData(string name, int slot)
    {
        UnityWebRequest request = UnityWebRequest.Get(API_URL + name);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var pokemon = JsonUtility.FromJson<PokemonData>(request.downloadHandler.text);
            DisplayPokemonData(pokemon, slot);
        }
        else
        {
            Debug.LogError("Error fetching pokemon data: " + request.error);
        }
    }

    void DisplayPokemonData(PokemonData pokemon, int slot)
    {
        string types = string.Join(", ", pokemon.types.Select(t => t.type.name.FirstCharacterToUpper()));

        string stats = $"Name: {pokemon.name.FirstCharacterToUpper()}\n\n" +
                       $"Types: {types}\n\n" +
                       $"HP: {pokemon.stats.FirstOrDefault(s => s.stat.name == "hp").base_stat}\n" +
                       $"Attack: {pokemon.stats.FirstOrDefault(s => s.stat.name == "attack").base_stat}\n" +
                       $"Defence: {pokemon.stats.FirstOrDefault(s => s.stat.name == "defense").base_stat}\n" +
                       $"Special Attack: {pokemon.stats.FirstOrDefault(s => s.stat.name == "special-attack").base_stat}\n" +
                       $"Special Defence: {pokemon.stats.FirstOrDefault(s => s.stat.name == "special-defense").base_stat}\n" +
                       $"Speed: {pokemon.stats.FirstOrDefault(s => s.stat.name == "speed").base_stat}\n";

        if (slot == 1)
        {
            pokemon1StatText.text = stats;
            StartCoroutine(PokemonUtility.DisplaySprite(pokemon.sprites.front_default, pokemon1Sprite));
            PokemonUtility.UpdateStatBars(pokemon.stats, pokemon1StatBars);
        }
        else if (slot == 2)
        {
            pokemon2StatText.text = stats;
            StartCoroutine(PokemonUtility.DisplaySprite(pokemon.sprites.front_default, pokemon2Sprite));
            PokemonUtility.UpdateStatBars(pokemon.stats, pokemon2StatBars);
        }

    }
}
