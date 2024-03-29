﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace ContousCookbook.Data
{
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class SampleDataItem
    {
        public SampleDataItem(
            String uniqueId,
            String title,
            String subtitle,
            String imagePath,
            String description,
            String content,
            double preparationTime,
            double rating,
            bool favorite,
            string tileImagePath,
            ObservableCollection<string> ingredients,
            SampleDataGroup group
            )
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Content = content;
            this.PreparationTime = preparationTime;
            this.Rating = rating;
            this.Favorite = favorite;
            this.TileImagePath = tileImagePath;
            this.Ingredients = ingredients;
            this.Group = group;
            this.Media = new ObservableCollection<StorageFile>();
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public string Content { get; private set; }
        public double PreparationTime { get; private set; }
        public double Rating { get; private set; }
        public bool Favorite { get; private set; }
        public string TileImagePath { get; private set; }
        public ObservableCollection<string> Ingredients { get; private set; }
        public SampleDataGroup Group { get; private set; }
        public ObservableCollection<StorageFile> Media { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class SampleDataGroup
    {
        public SampleDataGroup(
            String uniqueId,
            String title,
            String subtitle,
            String imagePath,
            String description,
            string groupImagePath,
            string groupHeaderImagePath
            )
        {
            this.UniqueId = uniqueId;
            this.Title = title;
            this.Subtitle = subtitle;
            this.Description = description;
            this.ImagePath = imagePath;
            this.Items = new ObservableCollection<SampleDataItem>();
            this.GroupHeaderImagePath = groupHeaderImagePath;
            this.GroupImagePath = GroupImagePath;
        }

        public string UniqueId { get; private set; }
        public string Title { get; private set; }
        public string Subtitle { get; private set; }
        public string Description { get; private set; }
        public string ImagePath { get; private set; }
        public ObservableCollection<SampleDataItem> Items { get; private set; }
        public string GroupImagePath { get; private set; }
        public string GroupHeaderImagePath { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with content read from a static json file.
    /// 
    /// SampleDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public sealed class SampleDataSource
    {
        private static SampleDataSource _sampleDataSource = new SampleDataSource();

        private ObservableCollection<SampleDataGroup> _groups = new ObservableCollection<SampleDataGroup>();
        public ObservableCollection<SampleDataGroup> Groups
        {
            get { return this._groups; }
        }

        public static async Task<IEnumerable<SampleDataGroup>> GetGroupsAsync()
        {
            await _sampleDataSource.GetSampleDataAsync();

            return _sampleDataSource.Groups;
        }

        public static async Task<SampleDataGroup> GetGroupAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.Groups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<SampleDataItem> GetItemAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task GetSampleDataAsync()
        {
            if (this._groups.Count != 0)
                return;

            Uri dataUri = new Uri("ms-appx:///DataModel/SampleData.json");

            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            string jsonText = await FileIO.ReadTextAsync(file);
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["Groups"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                SampleDataGroup group = new SampleDataGroup(groupObject["UniqueId"].GetString(),
                                                            groupObject["Title"].GetString(),
                                                            groupObject["Subtitle"].GetString(),
                                                            groupObject["ImagePath"].GetString(),
                                                            groupObject["Description"].GetString(),
                                                            groupObject["GroupImagePath"].GetString(),
                                                            groupObject["GroupHeaderImagePath"].GetString());

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    group.Items.Add(new SampleDataItem(itemObject["UniqueId"].GetString(),
                                                       itemObject["Title"].GetString(),
                                                       itemObject["Subtitle"].GetString(),
                                                       itemObject["ImagePath"].GetString(),
                                                       itemObject["Description"].GetString(),
                                                       itemObject["Content"].GetString(),
                                                       itemObject["PreparationTime"].GetNumber(),
                                                       itemObject["Rating"].GetNumber(),
                                                       itemObject["Favorite"].GetBoolean(),
                                                       itemObject["TileImagePath"].GetString(),
                                                       new ObservableCollection<string>(itemObject["Ingredients"].GetArray().Select(p => p.GetString())), group)
                    );
                }
                this.Groups.Add(group);
            }
        }

        public static async Task<SampleDataGroup> GetTopRatedRecipesAsync(int count)
        {
            await _sampleDataSource.GetSampleDataAsync();

            var favorites = new SampleDataGroup("TopRated", "Top Rated", "Top Rated Recipes", "", "Favorite recipes rated by our users.", "", "");
            var topRatedRecipes = _sampleDataSource.Groups.SelectMany(group => group.Items).OrderByDescending(recipe => recipe.Rating).Take(count);
            foreach (var recipe in topRatedRecipes)
            {
                favorites.Items.Add(recipe);
            }

            return favorites;
        }

        public static async Task<IEnumerable<SampleDataItem>> GetFavoriteRecipesAsync(int count)
        {
            await _sampleDataSource.GetSampleDataAsync();
            return _sampleDataSource.Groups.SelectMany(group => group.Items).Where(recipe => recipe.Favorite).Take(count);
        }

        public static IEnumerable<SampleDataGroup> Search(string searchText, bool titleOnly = false)
        {
            var query = searchText.ToUpperInvariant();
            _sampleDataSource.GetSampleDataAsync().Wait();
            return _sampleDataSource.Groups
                    .Select(group =>
                    {
                        var filteredGroup = new SampleDataGroup(group.UniqueId, group.Title, group.Subtitle, group.ImagePath, group.Description, group.GroupImagePath, group.GroupHeaderImagePath);

                        // add recipes that contain search text in title or content
                        foreach (var item in group.Items
                                    .Where(item => item.Title.ToUpperInvariant().Contains(query) || (!titleOnly && item.Content.ToUpperInvariant().Contains(query))))
                        {
                            filteredGroup.Items.Add(item);
                        }

                        return filteredGroup;
                    });
        }


    }
}