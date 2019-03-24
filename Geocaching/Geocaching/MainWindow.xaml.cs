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
        List<Person> persons = new List<Person>();
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
            persons = db.Person.ToList();
            foreach (var person in persons)
            {
                var pin = AddPin(ConvertGeoCoordinateToLocation(person.GeoCoordinate), HooverOnPersonPinShowTooltip(person), Colors.Blue, person.ID);

                pin.MouseDown += (s, a) =>
                {
                    // Handle click on person pin here.
                    activePerson = persons.First(p => p.ID == person.ID);
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

            //if ()

            /*
            if (activePerson == null)
            {
                
            }
            else if (activePerson != null)
            {

                var opacitatedPin = pushpins.Where(pp => (string)pp.Tag != activePerson.FirstName).ToList();
                foreach (var pin in opacitatedPin)
                {
                    pin.Opacity = 0.5;
                }

                var activePersonPin = pushpins.Single(pp => (string)pp.Tag == activePerson.FirstName);
                activePersonPin.Opacity = 1.0;
            }*/
        }

        private void OnMapLeftClick()
        {
            // Handle map click here.
            activePerson = null;
            UpdateMap();
        }

        private void OnAddGeocacheClick(object sender, RoutedEventArgs args)
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
            int geocacheId = AddGeocacheToDatabase(dialog, latestClickLocation);
            var pin = AddPin(latestClickLocation, geocacheId.ToString(), Colors.Gray, geocacheId);

            pin.MouseDown += (s, a) =>
            {
                // Handle click on geocache pin here.
                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
        }

        private int AddGeocacheToDatabase(GeocacheDialog dialog, Location latestClickLocation)
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
            int geocacheId = geocache.ID;

            geocaches.Add(geocache);

            return geocacheId;
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
            int personId = AddPersonToDatabase(dialog, latestClickLocation);

            // CreatePersonObject() takes the latestClickLocation, dialog and the personId
            // and converts it to a Person object.
            var pin = AddPin(latestClickLocation, HooverOnPersonPinShowTooltip(CreatePersonObject(latestClickLocation, dialog, personId)), Colors.Blue, personId);

            pin.MouseDown += (s, a) =>
            {
                // Handle click on person pin here.
                activePerson = persons.First(p => p.ID == personId);
                UpdateMap();

                // Prevent click from being triggered on map.
                a.Handled = true;
            };
        }

        private int AddPersonToDatabase(PersonDialog dialog, Location latestClickLocation)
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
            persons.Add(person);

            int id = person.ID;
            return id;
        }

        private Pushpin AddPin(Location location, string tooltip, Color color, int id)
        {
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            pin.Tag = id;
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
