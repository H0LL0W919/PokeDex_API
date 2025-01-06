using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public static class PokemonUtility
{
    private const string ALL_POKEMON_URL = "https://pokeapi.co/api/v2/pokemon?limit=1302";

    private const int maxStatValue = 255;

    public static IEnumerator GetAllPokémonNames(System.Action<List<string>> callback)
    {
        UnityWebRequest request = UnityWebRequest.Get(ALL_POKEMON_URL);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var allPokemon = JsonUtility.FromJson<AllPokemonList>(request.downloadHandler.text);
            var pokemonNames = allPokemon.results.Select(p => p.name).ToList();
            callback(pokemonNames);
        }
        else
        {
            Debug.LogError("Failed to fetch pokemon names: " + request.error);
            callback(new List<string>()); //passing empty list in case of error to avoid adding duplicates to the list.
        }
    }

    public static void DisplayNameSuggestions(string input, List<string> pokemonNames, Dropdown dropdown)
    {
        if (string.IsNullOrEmpty(input))
        {
            dropdown.gameObject.SetActive(true);
            return;
        }

        var suggestions = pokemonNames.Where(name => name.StartsWith(input.ToLower()))
                          .Take(10).ToList();

        if (suggestions.Count > 0)
        {
            dropdown.gameObject.SetActive(true);
            dropdown.ClearOptions();
            dropdown.AddOptions(suggestions);
        }
        else
        {
            dropdown.gameObject.SetActive(false);
        }

    }

    public static void SetPokémonName(int index, Dropdown dropdown, InputField inputField)
    {
        if (index >= 0 && index < dropdown.options.Count)
        {
            inputField.text = dropdown.options[index].text;
            dropdown.gameObject.SetActive(false);
        }
    }

    public static IEnumerator DisplaySprite(string url, Image targetImage)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);

            texture.filterMode = FilterMode.Point; //sharp edges
            texture.Apply();

            targetImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
        else
        {
            Debug.LogError("Error loading sprite: " + request.error);
        }
    }

    public static float NormaliseStats(string statName, Stat[] stats)
    {
        var stat = stats.FirstOrDefault(s => s.stat.name == statName);
        return stat != null ? (stat.base_stat / (float)maxStatValue) * 100f : 0f;
    }

    public static void UpdateStatBars(Stat[] stats, Slider[] statBars)
    {
        if (statBars == null || statBars.Length < 6) return;

        statBars[0].value = NormaliseStats("hp", stats);
        statBars[1].value = NormaliseStats("attack", stats);
        statBars[2].value = NormaliseStats("defense", stats);
        statBars[3].value = NormaliseStats("special-attack", stats);
        statBars[4].value = NormaliseStats("special-defense", stats);
        statBars[5].value = NormaliseStats("speed", stats);
    }
}

[System.Serializable]
public class AllPokemonList
{
    public List<PokemonName> results;
}

[System.Serializable]
public class PokemonName
{
    public string name;
}
