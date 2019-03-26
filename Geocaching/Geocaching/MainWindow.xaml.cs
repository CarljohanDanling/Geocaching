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
    }

    public class FoundGeocache
    {
        public int PersonID { get; set; }
        public Person Person { get; set; }

        public int GeocacheID { get; set; }
        public Geocache Geocache { get; set; }
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
        // The Location object in turn contains information like longitude and latitude.

        private Location latestClickLocation;
        private Location gothenburg = new Location(57.719021, 11.991202);

        Person activePerson;
        List<Geocache> geocaches = new List<Geocache>();
        List<Pushpin> pushpins = new List<Pushpin>();

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (applicationId == null)
            {
                MessageBox.Show("Please set the applicationId variable before running this program.");
                Environment.Exit(0);
            }

            CreateMap();

            // Load data from database and populate map here.
            //persons = db.Person.Include(p => p.GeoCoordinate).ToList();
            geocaches = db.Geocache.Include(g => g.FoundGeocache).Include(p => p.Person).ToList();

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

            foreach (var geocache in geocaches)
            {
                var pin = AddPin(ConvertGeoCoordinateToLocation(geocache.GeoCoordinate), HooverOnGeocachePinShowToolTip(geocache), Colors.Gray, geocache);

                pin.MouseDown += (s, a) =>
                {
                    // Handle click on person pin here.
                    var clickedGeocache = db.Geocache.First(p => p.ID == geocache.ID);
                    UpdateMap();

                    // Prevent click from being triggered on map.
                    a.Handled = true;
                };
            }
        }

        private void CreateMap()
        {
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = gothenburg;
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

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
                            }
                            else
                            {
                                pin.Background = new SolidColorBrush(Colors.Gray);
                            }
                        }
                    }
                }

                // Finds the clicked person's found geocaches.
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
                            }
                        }
                    }
                }
            }

            // If no person is selected, this code runs. Resets colors and opacity of all pins.
            else if (activePerson == null)
            {
                foreach (var pin in pushpins)
                {
                    pin.Opacity = 1;
                    if (pin.Tag.GetType() == typeof(Person))
                    {
                        pin.Background = new SolidColorBrush(Colors.Blue);
                    }
                    else
                    {
                        pin.Background = new SolidColorBrush(Colors.Gray);
                    }
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
                var pin = AddPin(latestClickLocation, HooverOnGeocachePinShowToolTip(geocache), Colors.Gray, geocache);

                pin.MouseDown += (s, a) =>
                {
                    // Handle click on geocache pin here.
                    UpdateMap();

                    // Prevent click from being triggered on map.
                    a.Handled = true;
                };
            }
        }

        private Geocache AddGeocacheToDatabase(GeocacheDialog dialog, Location latestClickLocation)
        {
            Geocache geocache = new Geocache();
            geocache.PersonID = activePerson.ID;
            geocache.GeoCoordinate = new Coordinate()
            {
                Latitude = latestClickLocation.Latitude,
                Longitude = latestClickLocation.Longitude
            };
            geocache.Contents = dialog.GeocacheContents;
            geocache.Message = dialog.GeocacheMessage;

            db.Add(geocache);
            db.SaveChanges();

            geocaches.Add(geocache);

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

            // CreatePersonObject() takes the latestClickLocation, dialog and the personId
            // and converts it to a Person object.
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

        private Pushpin AddPin(Location location, string tooltip, Color color, object something)
        {
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            pin.Tag = something;
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

        // This method is connected to the manually added persons. We need it because
        // the method that returns tooltip string requires a Person object.
        private Person CreatePersonObject(Location coordinates, PersonDialog dialog, int personId)
        {
            Person person = new Person()
            {
                ID = personId,
                FirstName = dialog.PersonFirstName,
                LastName = dialog.PersonLastName,
            };
            person.GeoCoordinate = new Coordinate()
            {
                Latitude = coordinates.Latitude,
                Longitude = coordinates.Longitude
            };
            person.Address = new Address()
            {
                City = dialog.AddressCity,
                Country = dialog.AddressCountry,
                StreetName = dialog.AddressStreetName,
                StreetNumber = dialog.AddressStreetNumber
            };

            return person;
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
        }

    }
}
