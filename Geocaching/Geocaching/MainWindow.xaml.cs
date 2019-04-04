using Microsoft.EntityFrameworkCore;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Device.Location;
using System.IO;

namespace Geocaching
{
    public class Person
    {
        [Key]
        public int ID { get; set; }
        [Required, MaxLength(50)]
        public string FirstName { get; set; }
        [Required, MaxLength(50)]
        public string LastName { get; set; }
        [Required]
        public Coordinate GeoCoordinate { get; set; }
        [Required]
        public Address Address { get; set; }
        public List<Geocache> Geocaches { get; set; }

        public List<FoundGeocache> FoundGeocache { get; set; }

        // Override ToString in order to format the output to SavetoFile
        public override string ToString()
        {
            string output = $"{FirstName} | {LastName} | {Address.Country}" +
                $" | {Address.City} | {Address.StreetName} | {Address.StreetNumber}" +
                $" | {GeoCoordinate.Latitude} | {GeoCoordinate.Longitude}";
            return output;
        }
    }

    public class Coordinate : GeoCoordinate
    {
        [Required]
        public new double Latitude { get; set; }
        [Required]
        public new double Longitude { get; set; }
    }

    public class Address
    {
        [Required, MaxLength(50)]
        public string Country { get; set; }
        [Required, MaxLength(50)]
        public string City { get; set; }
        [Required, MaxLength(50)]
        public string StreetName { get; set; }
        [Required]
        public byte StreetNumber { get; set; }
    }

    public class Geocache
    {
        [Key]
        public int ID { get; set; }
        public int? PersonID { get; set; }
        public Person Person { get; set; }
        [Required]
        public Coordinate GeoCoordinate { get; set; }
        [Required, MaxLength(255)]
        public string Contents { get; set; }
        [Required, MaxLength(255)]
        public string Message { get; set; }

        public List<FoundGeocache> FoundGeocache { get; set; }

        // Override ToString in order to format the output to SavetoFile
        public override string ToString()
        {
            string output = $"{ID} | {GeoCoordinate.Latitude}" +
                $" | {GeoCoordinate.Longitude} | {Contents} | { Message}";
            return output;
        }
    }

    public class FoundGeocache
    {
        public int PersonID { get; set; }
        public Person Person { get; set; }

        public int GeocacheID { get; set; }
        public Geocache Geocache { get; set; }

        // Help function to format the output to SavetoFile
        public static string CreateOutputString(FoundGeocache[] foundcaches)
        {
            string outputString = "Found: ";
            for (int i = 0; i < foundcaches.Length; i++)
            {
                outputString += foundcaches[i].GeocacheID;
                if (i < foundcaches.Length - 1)
                {
                    outputString += ", ";
                }
            }
            return outputString;
        }
    }

    class AppDbContext : DbContext
    {
        public DbSet<Person> Person { get; set; }
        public DbSet<Geocache> Geocache { get; set; }
        public DbSet<FoundGeocache> FoundGeocache { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS;Initial Catalog=Geocache;Integrated Security=True");
        }

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<FoundGeocache>()
                .HasKey(fg => new { fg.PersonID, fg.GeocacheID });

            model.Entity<Person>().OwnsOne(p => p.Address, a =>
            {
                a.Property(p => p.Country).HasColumnName("Country");
                a.Property(p => p.City).HasColumnName("City");
                a.Property(p => p.StreetName).HasColumnName("StreetName");
                a.Property(p => p.StreetNumber).HasColumnName("StreetNumber");
            });

            model.Entity<Person>().OwnsOne(g => g.GeoCoordinate, c =>
            {
                c.Property(g => g.Latitude).HasColumnName("Latitude");
                c.Property(g => g.Longitude).HasColumnName("Longitude");
                c.Ignore(g => g.Altitude);
                c.Ignore(g => g.Course);
                c.Ignore(g => g.HorizontalAccuracy);
                c.Ignore(g => g.Speed);
                c.Ignore(g => g.VerticalAccuracy);
            });

            model.Entity<Geocache>().OwnsOne(g => g.GeoCoordinate, c =>
           {
               c.Property(g => g.Latitude).HasColumnName("Latitude");
               c.Property(g => g.Longitude).HasColumnName("Longitude");
               c.Ignore(g => g.Altitude);
               c.Ignore(g => g.Course);
               c.Ignore(g => g.HorizontalAccuracy);
               c.Ignore(g => g.Speed);
               c.Ignore(g => g.VerticalAccuracy);
           });
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Contains the ID string needed to use the Bing map.
        // Instructions here: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/getting-a-bing-maps-key
        private const string applicationId = "AtY7XCl8HtaG0nheNNj7W3ryXegOUlT-CHea15PMBLkGmNF1hR6K5NJ04SZEcF0z";

        private AppDbContext db = new AppDbContext();

        private MapLayer layer;

        // Contains the location of the latest click on the map.
        private Location latestClickLocation;

        // The Location object in turn contains information like longitude and latitude.
        private Location gothenburg = new Location(57.719021, 11.991202);

        // The clicked person-pin on the map is assigned to this person object.
        Person activePerson;

        // List with all pushpins on map.
        List<Pushpin> pushpins = new List<Pushpin>();

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void ReadPersonAndGeocacheFromDatabase()
        {
            foreach (var person in db.Person)
            {
                var pin = AddPin(ConvertGeoCoordinateToLocation(person.GeoCoordinate), HooverOnPersonPinShowTooltip(person), Colors.Blue, person);

                pin.MouseDown += (s, a) =>
                {
                    // Handle click on person pin here.
                    activePerson = db.Person.First(p => p.ID == person.ID);
                    UpdateMap();

                    // Prevent click from being triggered on map.
                    a.Handled = true;
                };
            }

            foreach (var geocache in db.Geocache.Include(g => g.FoundGeocache).Include(p => p.Person))
            {
                var pin = AddPin(ConvertGeoCoordinateToLocation(geocache.GeoCoordinate), HooverOnGeocachePinShowToolTip(geocache), Colors.Gray, geocache);

                pin.MouseDown += (s, a) =>
                {
                    // Handle click on person pin here.
                    var clickedGeocache = db.Geocache.First(p => p.ID == geocache.ID);
                    ClickedGeochachePin(clickedGeocache);

                    // Prevent click from being triggered on map.
                    a.Handled = true;
                };
            }
            UpdateMap();
        }

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (applicationId == null)
            {
                MessageBox.Show("Please set the applicationId variable before running this program.");
                Environment.Exit(0);
            }

            // Load data from database and populate map here.
            CreateMap();
            ReadPersonAndGeocacheFromDatabase();
        }

        private void CreateMap()
        {
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = gothenburg;
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);
            WindowState = WindowState.Maximized;

            MouseDown += (sender, e) =>
            {
                var point = e.GetPosition(this);
                latestClickLocation = map.ViewportPointToLocation(point);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    OnMapLeftClick();
                }
            };

            map.ContextMenu = new ContextMenu();

            var addPersonMenuItem = new MenuItem { Header = "Add Person" };
            map.ContextMenu.Items.Add(addPersonMenuItem);
            addPersonMenuItem.Click += OnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClick;
        }

        private void UpdateMap()
        {
            List<Pushpin> notFoundPins = new List<Pushpin>();
            notFoundPins = pushpins.ToList();
            // It is recommended (but optional) to use this method for setting the color and opacity of each pin after every user interaction that might change something.
            // This method should then be called once after every significant action, such as clicking on a pin, clicking on the map, or clicking a context menu option.

            // This code runs if a person-pin is clicked. This code fades all other person-pins.
            if (activePerson != null)
            {
                foreach (var pin in pushpins)
                {
                    if (pin.Tag.GetType() == typeof(Person))
                    {
                        Person person = (Person)pin.Tag;
                        if (person.ID != activePerson.ID) pin.Opacity = 0.5;
                        else pin.Opacity = 1;
                    }
                }

                // Finds the clicked person's placed geocaches.
                if (activePerson.Geocaches != null)
                {
                    foreach (var placedCache in activePerson.Geocaches)
                    {
                        foreach (var pin in pushpins)
                        {
                            if (pin.Tag.GetType() == typeof(Geocache))
                            {
                                Geocache geocache = (Geocache)pin.Tag;
                                if (placedCache.PersonID == geocache.PersonID)
                                {
                                    pin.Background = new SolidColorBrush(Colors.Black);
                                    notFoundPins.Remove(pin);
                                }
                                else
                                {
                                    pin.Background = new SolidColorBrush(Colors.Gray);
                                }
                            }
                        }
                    }
                }

                // Colors the clicked person's found geocaches.
                if (activePerson.FoundGeocache != null)
                {
                    foreach (var foundCache in activePerson.FoundGeocache)
                    {
                        foreach (var pin in pushpins)
                        {
                            if (pin.Tag.GetType() == typeof(Geocache))
                            {
                                Geocache geocache = (Geocache)pin.Tag;
                                if (foundCache.GeocacheID == geocache.ID)
                                {
                                    pin.Background = new SolidColorBrush(Colors.Green);
                                    notFoundPins.Remove(pin);
                                }
                            }
                        }
                    }
                }

                // Colors the clicked person's NOT found geocaches.
                foreach (var pin in notFoundPins)
                {
                    if (pin.Tag.GetType() == typeof(Geocache))
                    {
                        Geocache geocache = (Geocache)pin.Tag;
                        pin.Background = new SolidColorBrush(Colors.Red);
                    }
                }
            }

            // If no person is selected, this code runs. Resets colors and opacity of all pins.
            else
            {
                foreach (var pin in pushpins)
                {
                    pin.Opacity = 1;

                    pin.Background = pin.Tag.GetType() == typeof(Person) ?
                        pin.Background = new SolidColorBrush(Colors.Blue) :
                        pin.Background = new SolidColorBrush(Colors.Gray);
                }
            }
        }

        private void OnMapLeftClick()
        {
            // Handle map click here.
            activePerson = null;
            UpdateMap();
        }

        private void OnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            if (activePerson == null)
            {
                MessageBox.Show("Please select a person before adding a geocache.");
            }
            else
            {
                var dialog = new GeocacheDialog();
                dialog.Owner = this;
                dialog.ShowDialog();
                if (dialog.DialogResult == false)
                {
                    return;
                }

                string contents = dialog.GeocacheContents;
                string message = dialog.GeocacheMessage;

                // Add geocache to map and database here.
                Geocache geocache = AddGeocacheToDatabase(dialog, latestClickLocation);
                var pin = AddPin(latestClickLocation, HooverOnGeocachePinShowToolTip(geocache), Colors.Black, geocache);

                pin.MouseDown += (s, a) =>
                {
                    // Handle click on geocache pin here.
                    var clickedGeocache = db.Geocache.First(p => p.ID == geocache.ID);
                    ClickedGeochachePin(clickedGeocache);

                    // Prevent click from being triggered on map.
                    a.Handled = true;
                };
            }
        }

        private Geocache AddGeocacheToDatabase(GeocacheDialog dialog, Location latestClickLocation)
        {
            Geocache geocache = new Geocache()
            {
                PersonID = activePerson.ID,
                GeoCoordinate = new Coordinate()
                {
                    Latitude = latestClickLocation.Latitude,
                    Longitude = latestClickLocation.Longitude
                },
                Contents = dialog.GeocacheContents,
                Message = dialog.GeocacheMessage
            };

            db.Add(geocache);
            db.SaveChanges();

            return geocache;
        }

        private void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            var dialog = new PersonDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            }

            string firstName = dialog.PersonFirstName;
            string lastName = dialog.PersonLastName;
            string city = dialog.AddressCity;
            string country = dialog.AddressCountry;
            string streetName = dialog.AddressStreetName;
            int streetNumber = dialog.AddressStreetNumber;

            // Add person to map and database here.
            Person person = AddPersonToDatabase(dialog, latestClickLocation);

            var pin = AddPin(latestClickLocation, HooverOnPersonPinShowTooltip(person), Colors.Blue, person);

            pin.MouseDown += (s, a) =>
            {
                // Handle click on person pin here.
                activePerson = db.Person.First(p => p.ID == person.ID);
                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
        }

        private Person AddPersonToDatabase(PersonDialog dialog, Location latestClickLocation)
        {
            Person person = new Person();
            person.FirstName = dialog.PersonFirstName;
            person.LastName = dialog.PersonLastName;
            person.Address = new Address()
            {
                City = dialog.AddressCity,
                StreetName = dialog.AddressStreetName,
                StreetNumber = dialog.AddressStreetNumber,
                Country = dialog.AddressCountry
            };
            person.GeoCoordinate = new Coordinate()
            {
                Longitude = latestClickLocation.Longitude,
                Latitude = latestClickLocation.Latitude
            };
            db.Add(person);
            db.SaveChanges();

            return person;
        }

        private void ClickedGeochachePin(Geocache geocache)
        {
            if (activePerson != null && activePerson.Geocaches == null || activePerson != null && !activePerson.Geocaches.Contains(geocache))
            {
                var listOfIds = db.FoundGeocache.Where(fg => fg.PersonID == activePerson.ID).Select(fg => fg.GeocacheID).ToList();

                if (listOfIds.Contains(geocache.ID))
                {
                    var foundGeocacheToDelete = db.FoundGeocache.First(fg => (fg.PersonID == activePerson.ID) && (fg.GeocacheID == geocache.ID));
                    db.Remove(foundGeocacheToDelete);
                    db.SaveChanges();
                }
                else
                {
                    FoundGeocache foundgeocache = new FoundGeocache()
                    {
                        PersonID = activePerson.ID,
                        GeocacheID = geocache.ID
                    };
                    db.Add(foundgeocache);
                    db.SaveChanges();
                }
            }
            UpdateMap();
        }

        private Pushpin AddPin(Location location, string tooltip, Color color, object typeOfObject)
        {
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            pin.Tag = typeOfObject;
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, new Location(location.Latitude, location.Longitude));
            pushpins.Add(pin);
            return pin;
        }

        private Location ConvertGeoCoordinateToLocation(Coordinate geoCoordinate)
        {
            Location location = new Location()
            {
                Latitude = geoCoordinate.Latitude,
                Longitude = geoCoordinate.Longitude
            };
            return location;
        }

        private string HooverOnPersonPinShowTooltip(Person person)
        {
            return ($"{person.FirstName} {person.LastName}\n{person.Address.StreetName} {person.Address.StreetNumber}\n{person.Address.City}");
        }

        private string HooverOnGeocachePinShowToolTip(Geocache geocache)
        {
            return ($"Latitude: {geocache.GeoCoordinate.Latitude}\n" +
                    $"Longitude: {geocache.GeoCoordinate.Longitude}\n" +
                    $"Message: {geocache.Message}\n" +
                    $"Content: {geocache.Contents}\n" +
                    $"Person placed geocache: {geocache.Person.FirstName} {geocache.Person.LastName}");
        }

        private void OnLoadFromFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;

            // Read the selected file here.
            Dictionary<Person, List<int>> personFoundGeocaches = new Dictionary<Person, List<int>>();
            Dictionary<int, Geocache> specificGeocache = new Dictionary<int, Geocache>();

            string[] lines = File.ReadAllLines(path, Encoding.GetEncoding("ISO-8859-1")).ToArray();
            int counterPersonObject = 0;
            int emptyLineCounter = 0;

            Person person = new Person();
            Geocache geocache = new Geocache();

            db.Person.RemoveRange(db.Person);
            db.Geocache.RemoveRange(db.Geocache);
            db.SaveChanges();

            foreach (string line in lines)
            {
                if (!line.Contains("Found"))
                {
                    string[] values = line.Split('|').Select(v => v.Trim()).ToArray();

                    if (values[0] != "" && counterPersonObject == 0)
                    {
                        person = new Person();
                        person.FirstName = values[0];
                        person.LastName = values[1];

                        person.Address = new Address
                        {
                            Country = values[2],
                            City = values[3],
                            StreetName = values[4],
                            StreetNumber = byte.Parse(values[5])
                        };
                        person.GeoCoordinate = new Coordinate
                        {
                            Latitude = double.Parse(values[6]),
                            Longitude = double.Parse(values[7])
                        };
                        db.Person.Add(person);
                        db.SaveChanges();

                        emptyLineCounter = 0;
                        counterPersonObject++;
                    }

                    else if (values[0] == "")
                    {
                        counterPersonObject = 0;
                        emptyLineCounter++;

                        if (emptyLineCounter == 2)
                        {
                            return;
                        }
                    }

                    else
                    {
                        geocache = new Geocache();

                        int geocacheNumber = int.Parse(values[0]);
                        geocache.GeoCoordinate = new Coordinate
                        {
                            Latitude = double.Parse(values[1]),
                            Longitude = double.Parse(values[2])
                        };
                        geocache.Contents = values[3];
                        geocache.Message = values[4];
                        geocache.Person = person;

                        db.Geocache.Add(geocache);
                        db.SaveChanges();

                        specificGeocache.Add(geocacheNumber, geocache);
                    }
                }

                else
                {
                    string[] geocachesFound = line.Split(':', ',').Skip(1).Select(v => v.Trim()).ToArray();
                    List<int> geocachesId = new List<int>();
                    foreach (var item in geocachesFound)
                    {
                        int geocacheId = int.Parse(item);
                        geocachesId.Add(geocacheId);
                    }
                    personFoundGeocaches.Add(person, geocachesId);
                }
            }

            foreach (var personObject in personFoundGeocaches.Keys)
            {
                foreach (int geocacheId in personFoundGeocaches[personObject])
                {
                    var geocacheObject = specificGeocache[geocacheId];
                    FoundGeocache foundGeocache = new FoundGeocache
                    {
                        PersonID = personObject.ID,
                        GeocacheID = geocacheObject.ID
                    };
                    db.Add(foundGeocache);
                    db.SaveChanges();
                };
            }
            pushpins.Clear();
            layer.Children.Clear();
            activePerson = null;
            ReadPersonAndGeocacheFromDatabase();
        }

        private void OnSaveToFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            dialog.FileName = "Geocaches";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;
            // Write to the selected file here.

            List<string> fileLines = new List<string>();

            Person[] persons = db.Person.OrderByDescending(p => p).ToArray();

            foreach (Person person in persons)
            {
                fileLines.Add(person.ToString());

                Geocache[] geocaches = db.Geocache.
                            Where(g => g.PersonID == person.ID).
                            OrderByDescending(a => a).ToArray();

                foreach (var geocache in geocaches)
                {
                    fileLines.Add(geocache.ToString());
                }

                FoundGeocache[] personFoundGeocaches = db.FoundGeocache.
                            Where(fg => fg.PersonID == person.ID).
                            OrderByDescending(a => a).ToArray();

                fileLines.Add(FoundGeocache.CreateOutputString(personFoundGeocaches));
                fileLines.Add("");
            }
            //fileLines.RemoveAt(fileLines.Count() - 1);
            File.WriteAllLines(path, fileLines);
        }
    }
}
