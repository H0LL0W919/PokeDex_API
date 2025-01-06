using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//json utility requires classes to be public and serializable
public class Pokedex : MonoBehaviour
{
    [Header("User Input Box")]
    public InputField userInput;

    [Header("UI Buttons")]
    public Button searchButton;
    public Button nextButton;
    public Button prevButton;
    public Button moveScreenButton;

    [Header("Pokemon Name Selection")]
    public Dropdown dropDown;

    [Header("UI Text Objects")]
    public Text id;
    public Text nameText;
    public Text typesText;
    public Text heightText;
    public Text weightText;
    public Text statText;
    public Text weaknessText;
    public Text strengthText;

    [Header("Sprite Plug-in")]
    public Image spriteImage;

    [Header("Stats")]
    public Slider[] pokemonStatBars;

    [Header("Region ScrollView")]
    public GameObject regionScrollView;
    public GameObject regionTextPrefab;
    public Transform regionContent;
    
    [Header("API Endpoints")]
    private const string API_URL = "https://pokeapi.co/api/v2/pokemon/";  //Setting the endpoint of our API in a constant variable

    [Header("Next/Prev Button variables")]
    private const int firstPokemon = 1;
    private const int lastPokemon = 1025;
    private int currentPokemonID = 1; //tracks selected pokemons ID

    [Header("Pokemon Name List for Predictive Text/Suggestions")]
    private List<string> pokemonNames = new List<string>(); //stores all pokemon names

    void Start()
    {
        searchButton.onClick.AddListener(SearchButton); //Adds event listener and when the search button is clicked it will call the function
        nextButton.onClick.AddListener(NextButton);
        prevButton.onClick.AddListener(PreviousButton);
        moveScreenButton.onClick.AddListener(OpenMovesScene);

        userInput.onValueChanged.AddListener(OnInputFieldChanged); //autocomplete trigger
        dropDown.onValueChanged.AddListener(OnSuggestionSelected); //handle dropdown selection
        dropDown.gameObject.SetActive(false); //hides suggestions initially

        FetchAllPokemonNames(); //fetches all names for suggestion text
        FetchPokemonByID(currentPokemonID); //loads Bulbasaur (#1) by default
    }

    void SearchButton()
    {
        string search = userInput.text.ToLower(); //changing input text to lower case so the api request goes through
        if (!string.IsNullOrEmpty(search))
        {
            StartCoroutine(FetchPokemonData(search));
        }
    }

    void NextButton()
    {
        currentPokemonID++;

        if (currentPokemonID > lastPokemon)
        {
            currentPokemonID = firstPokemon; //wrapping around to first pokemon
        }

        FetchPokemonByID(currentPokemonID);
    }

    void PreviousButton()
    {
        currentPokemonID--;

        if (currentPokemonID < firstPokemon)
        {
            currentPokemonID = lastPokemon; //wrap around to last pokemon
        }

        FetchPokemonByID(currentPokemonID);
    }

    void FetchPokemonByID(int id)
    {
        StartCoroutine(FetchPokemonData(id.ToString())); //Fetching pokemon by id
    }

    IEnumerator FetchPokemonData(string search) //search is the pokemon the person searched for
    {
        UnityWebRequest request = UnityWebRequest.Get(API_URL + search); //sends request to api's url + entered pokemon name or id
        yield return request.SendWebRequest(); //waits for web request to complete before moving on

        if (request.result == UnityWebRequest.Result.Success)
        {
            dropDown.gameObject.SetActive(false); //hiding suggestions
            DisplayData(request.downloadHandler.text); //retrieves JSON response from API
        }
        else
        {
            Debug.LogError("Error: " + request.error);
            nameText.text = "Error: Pokemon Not Found!"; //if there's a connection error or protocol error a log message will print to console
        }
    }

    private string currentPokemonName;

    void DisplayData(string json)
    {
        //Deserializing json into a dynamic object
        var pokemon = JsonUtility.FromJson<PokemonData>(json); //creates a pokemon variables which holds Pokemon Data objects listed in class which are extracted from Json

        currentPokemonID = pokemon.id;
        currentPokemonName = pokemon.name;

        //updating UI with Data
        id.text = "ID: #" + pokemon.id;
        nameText.text = "Name: " + pokemon.name.ToUpper();
        typesText.text = "Type(s): " + string.Join(", ", pokemon.types.Select(t => t.type.name)).ToUpper(); //Array of typewrapper objs, each typewrapper contains typedetail obj which has a name property, t = each item in pokemon.types, t.type accesses typedetail & t.type.name accesses the name property of the type e.g. fire, water
        heightText.text = "Height: " + (pokemon.height / 10) + "m"; //data returns decimetres
        weightText.text = "Weight: " + (pokemon.weight / 10) + "kg"; //data returns hectograms 
        statText.text = "Base Stats: \n" + string.Join("\n", pokemon.stats.Select(s => s.stat.name + ": " + s.base_stat)).ToUpper(); //s represents each item in pokemon.stats, .name provides name of stat beside the base stats value

        string[] types = pokemon.types.Select(t => t.type.name).ToArray();
        FetchTypeWeaknesses(types);
        FetchTypeStrengths(types);

        //update sliders with stat values
        PokemonUtility.UpdateStatBars(pokemon.stats, pokemonStatBars);

        //load and display sprites
        StartCoroutine(PokemonUtility.DisplaySprite(pokemon.sprites.front_default, spriteImage));

        FetchRegionAvailability(currentPokemonID);
    }

    void FetchAllPokemonNames()
    {
        StartCoroutine(PokemonUtility.GetAllPokémonNames(names => pokemonNames = names));
    }

    void OnInputFieldChanged(string input)
    {
        PokemonUtility.DisplayNameSuggestions(input, pokemonNames, dropDown);
    }

    void OnSuggestionSelected(int index)
    {
        PokemonUtility.SetPokémonName(index, dropDown, userInput);
    }

    void FetchRegionAvailability(int pokemonID)
    {
        StartCoroutine(FetchRegionData(pokemonID));
    }

    IEnumerator FetchRegionData(int pokemonID)
    {
        string url = API_URL + pokemonID + "/encounters";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            ParseAndDisplayRegions(json);
        }
        else
        {
            Debug.LogError("Error fetching region data: " + request.error);
            
        }
    }

    void ParseAndDisplayRegions(string json)
    {
        string wrappedJson = "{\"locations\":" + json + "}"; //wrapping json in object to parse arrays

        var locationWrapper = JsonUtility.FromJson<LocationWrapper>(wrappedJson);

        foreach (Transform child in regionContent)
        {
            Destroy(child.gameObject);
        }

        if (locationWrapper != null && locationWrapper.locations.Count > 0)
        {
            var regionNames = locationWrapper.locations.Select(loc => loc.location_area.name)
                              .Distinct().Select(region => CultureInfo.CurrentCulture.TextInfo
                              .ToTitleCase(region.Replace("-", " ").Replace(" area", "")));

            foreach (var region in regionNames)
            {
                GameObject newText = Instantiate(regionTextPrefab, regionContent);
                newText.GetComponent<Text>().text = region;
            }
            
        }
        else
        {
            GameObject newText = Instantiate(regionTextPrefab, regionContent);
            newText.GetComponent<Text>().text = "Location Information Unknown...";
        }
    }

    void FetchTypeWeaknesses(string[] types)
    {
        StartCoroutine(FetchWeaknessData(types));
    }

    void FetchTypeStrengths(string[] types)
    {
        StartCoroutine(FetchStrengthData(types));
    }

    IEnumerator FetchStrengthData(string[] types)
    {
        HashSet<string> strengths = new HashSet<string>();

        foreach (string type in types)
        {
            string url = "https://pokeapi.co/api/v2/type/" + type;
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var typeData = JsonUtility.FromJson<TypeData>(request.downloadHandler.text);
                foreach (var strength in typeData.damage_relations.double_damage_to)
                {
                    strengths.Add(strength.name);
                }
            }
            else
            {
                Debug.LogError("Error fetching type data: " + request.error);
            }
        }

        strengthText.text = string.Join(", ", strengths.Select(s => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s)));

    }

    IEnumerator FetchWeaknessData(string[] types)
    {
        HashSet<string> weaknesses = new HashSet<string>(); //stroing unique weaknesses

        foreach (string type in types)
        {
            string url = "https://pokeapi.co/api/v2/type/" + type;
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var typeData = JsonUtility.FromJson<TypeData>(request.downloadHandler.text);
                foreach (var weakness in typeData.damage_relations.double_damage_from)
                {
                    weaknesses.Add(weakness.name); 
                }
            }
            else
            {
                Debug.LogError("Error fetching type data: " + request.error);
            }
        }

        weaknessText.text = string.Join(", ", weaknesses.Select(w => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(w)));
    }

    void OpenMovesScene()
    {
        if (!string.IsNullOrEmpty(currentPokemonName))
        {
            PlayerPrefs.SetString("SelectedPokemon", currentPokemonName.ToLower());
            SceneManager.LoadScene("MoveScene");
        }
        else
        {
            Debug.LogError("No Pokemon selected to view moves!");
        }  
    }

}

[System.Serializable]
public class PokemonData //Structure of pokemon data in the api response - data stored in these variables and displayed to user
{
    public int id;
    public string name;
    public float height;
    public float weight;
    public Stat[] stats; //array used due to having 6 base stat values
    public TypeWrapper[] types; // ^< 2 nested classes needed to match the JSONs structure for deserialization - JSON response contains nested objects.  Classes mirror the structure to pull the correct data out
    public Sprites sprites;
    public MoveWrapper[] moves;
}

[System.Serializable]
public class Stat
{
    public StatDetail stat;
    public int base_stat;
}

[System.Serializable]
public class StatDetail
{
    public string name;
}

[System.Serializable]
public class TypeWrapper
{
    public TypeDetail type;
    public string name;
}

[System.Serializable]
public class TypeDetail
{
    public string name;
}

[System.Serializable]
public class DamageRelations
{
    public List<TypeDetail> double_damage_from;
    public List<TypeDetail> double_damage_to;
}

[System.Serializable]
public class TypeData
{
    public DamageRelations damage_relations;
}

[System.Serializable]
public class Sprites
{
    public string front_default;
}

[System.Serializable]
public class EncounterLocation
{
    public LocationArea location_area;
}

[System.Serializable]
public class LocationArea
{
    public string name;
}

[System.Serializable]
public class LocationWrapper
{
    public List<EncounterLocation> locations;
}

//Code that was changed
//void SpellingSuggestion(string input)
//{
//    if (pokemonNames == null || pokemonNames.Count == 0)
//        return;

//    string closestName = pokemonNames.OrderBy(name => LevenshteinCalc(input, name)).FirstOrDefault();

//    suggestionText.text = "Did you mean: " + closestName + "?"; 
//}

//int LevenshteinCalc(string a, string b) //string similarity algorithm - a = source string - b = target string
//{
//    if (string.IsNullOrEmpty(a)) //edge cases
//        return b.Length;
//    if (string.IsNullOrEmpty(b))
//        return a.Length;

//    int[,] matrix = new int[a.Length + 1, b.Length + 1];

//    for (int i = 0; i <= a.Length; i++)
//        matrix[i, 0] = i;
//    for (int j = 0; j <= b.Length; j++) //initialising first row and collumn 
//        matrix[0, j] = j;

//    for (int i = 1; i <= a.Length; i++)
//    {
//        for (int j = 1; j <= b.Length; j++)
//        {
//            int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
//            matrix[i, j] = Mathf.Min(Mathf.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
//        }
//    }

//    return matrix[a.Length, b.Length]; //this algorithm works by calculating the distance between the source and target string.
//}