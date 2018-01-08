using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RavenSessionTrouble
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var store = new DocumentStore
            {
                Urls = new[] { "http://localhost:8080/" },
                Database = "trouble"
            })
            {
                store.Initialize();
                using (var session = store.OpenSession())
                {
                    for (int i = 0; i < 5; i++)
                    {
                        session.Store(new Person { Id = Guid.NewGuid().ToString(), Name = "name" });
                        session.Store(new Pet { Id = Guid.NewGuid().ToString(), Name = "name" });
                        session.SaveChanges();
                    }
                }

                var people = GetPeople(store);
                var changeNameTasks = people.Select(person => Task.Run(() => ChangeName(store, person))).ToArray();
                Task.WaitAll(changeNameTasks);
            }
        }

        private static async Task ChangeName(DocumentStore store, Person person)
        {
            using (var session = store.OpenAsyncSession())
            {
                await StreamSomething(store); // This line causes the program to hang
                person.Name = "newName";
                await session.StoreAsync(person, person.Id);
                await session.SaveChangesAsync();
            }
        }

        private static List<Person> GetPeople(DocumentStore store)
        {
            using (var session = store.OpenSession())
            {
                return session.Query<Person>().ToList();
            }
        }

        public static async Task StreamSomething(DocumentStore store)
        {
            using (var session = store.OpenAsyncSession())
            {
                var query = session.Query<Pet>();
                var stream = await session.Advanced.StreamAsync(query);
                while (await stream.MoveNextAsync())
                {
                    var item = stream.Current.Document;
                    // do nothing
                }
            }
        }
    }

    class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    class Pet
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
