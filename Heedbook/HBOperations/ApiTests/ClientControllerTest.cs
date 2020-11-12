using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System;
using HBData.Models;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using UserOperations.Models.AnalyticModels;
using UserOperations.Services;
using UserOperations.Models.Get.AnalyticServiceQualityController;
using UserOperations.Models.Get.AnalyticSpeechController;
using UserOperations.Models;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Internal;
using System.Linq.Expressions;

namespace ApiTests
{
    public class ClientControllerTests : ApiServiceTest
    {
        private ClientService clientService;
        private ClientNoteService clientNoteService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            clientService = new ClientService(
                moqILoginService.Object,
                requestFiltersMock.Object,
                repositoryMock.Object,
                fileRefUtils.Object);
            clientNoteService = new ClientNoteService(
                moqILoginService.Object,
                requestFiltersMock.Object,
                repositoryMock.Object
            );
        }
        [Test]
        public async Task GetAllTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            fileRefUtils.Setup(p => p.GetFileLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns("TestRef.com");
            repositoryMock.Setup(p => p.GetAsQueryable<Client>())
                .Returns(new TestAsyncEnumerable<Client>(new List<Client>
                {
                    new Client
                    {
                        ClientId = clientId,
                        Age = 17,
                        Dialogues = new List<Dialogue>
                        {
                            new Dialogue
                            {
                                DialogueId = Guid.NewGuid(),
                                BegTime = DateTime.Now.AddMinutes(-20),
                                EndTime = DateTime.Now.AddMinutes(-15),
                                StatusId = 3
                            }
                        },
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        Email = "Test@heedbook.com",
                        Name = "TestClient",
                        Gender = "male",
                        Phone = "88008888888",
                        StatusId = 3,
                        ClientNotes = new List<ClientNote>
                        {
                            new ClientNote
                            {
                                ClientNoteId = Guid.NewGuid(),
                                ApplicationUserId = userId,
                                ApplicationUser = new ApplicationUser
                                {
                                    FullName = "TestUser"
                                },
                                Text = "Client like red colour",
                                ClientId = clientId,
                                CreationDate = DateTime.Now.AddDays(-2) 
                            }
                        }
                    }
                }.AsQueryable()));

            //Act
            var clients = await clientService.GetAll(
                TestData.beg,
                TestData.end,
                new List<string>{"male", "female"},
                new List<Guid>{companyId},
                5,
                20);
            //Assert
            Assert.IsTrue(clients.Count > 0);
        }
        [Test]
        public async Task GetGetTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            repositoryMock.Setup(p => p.GetAsQueryable<Client>())
                .Returns(new TestAsyncEnumerable<Client>(new List<Client>
                {
                    new Client
                    {
                        ClientId = clientId,
                        Age = 17,
                        Dialogues = new List<Dialogue>
                        {
                            new Dialogue
                            {
                                DialogueId = Guid.NewGuid(),
                                BegTime = DateTime.Now.AddMinutes(-20),
                                EndTime = DateTime.Now.AddMinutes(-15),
                                StatusId = 3
                            }
                        },
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        Email = "Test@heedbook.com",
                        Name = "TestClient",
                        Gender = "male",
                        Phone = "88008888888",
                        StatusId = 3,
                        ClientNotes = new List<ClientNote>
                        {
                            new ClientNote
                            {
                                ClientNoteId = Guid.NewGuid(),
                                ApplicationUserId = userId,
                                ApplicationUser = new ApplicationUser
                                {
                                    FullName = "TestUser"
                                },
                                Text = "Client like red colour",
                                ClientId = clientId,
                                CreationDate = DateTime.Now.AddDays(-2) 
                            }
                        }
                    }
                }.AsQueryable()));
            fileRefUtils.Setup(p => p.GetFileLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns("TestRef.com");         
                       
            //Act
            var client = await clientService.Get(clientId);

            //Assert            
            Assert.IsFalse(client is null);
        }
        [Test]
        public async Task ClientUpdatePutTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.FindOrExceptionOneByConditionAsync<Client>(It.IsAny<Expression<Func<Client, bool>>>()))
                .Returns(Task.FromResult<Client>(
                    new Client
                    {
                        ClientId = clientId,
                        Age = 17,
                        Dialogues = new List<Dialogue>
                        {
                            new Dialogue
                            {
                                DialogueId = Guid.NewGuid(),
                                BegTime = DateTime.Now.AddMinutes(-20),
                                EndTime = DateTime.Now.AddMinutes(-15),
                                StatusId = 3
                            }
                        },
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        Email = "Test@heedbook.com",
                        Name = "TestClient",
                        Gender = "male",
                        Phone = "88008888888",
                        StatusId = 3,
                        ClientNotes = new List<ClientNote>
                        {
                            new ClientNote
                            {
                                ClientNoteId = Guid.NewGuid(),
                                ApplicationUserId = userId,
                                ApplicationUser = new ApplicationUser
                                {
                                    FullName = "TestUser"
                                },
                                Text = "Client like red colour",
                                ClientId = clientId,
                                CreationDate = DateTime.Now.AddDays(-2) 
                            }
                        }
                    }));
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.Update<Client>(It.IsAny<Client>()));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            var putClient = new PutClient
            {
                ClientId = clientId,
                Name = "TestClient",
                Phone = "8888888888",
                Gender = "male",
                Age = 40,
                Avatar = "TestAvatar.jpg",
                StatusId = 3
            };

            //Act
            var client = await clientService.Update(putClient);
            System.Console.WriteLine(JsonConvert.SerializeObject(client));
            //Assert            
            Assert.IsFalse(client is null);
        }
        [Test]
        public async Task ClientDeleteDelete()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.Delete<ClientNote>(It.IsAny<ClientNote>()));
            repositoryMock.Setup(p => p.Delete<Client>(It.IsAny<Client>()));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            repositoryMock.Setup(p => p.GetAsQueryable<Client>())
                .Returns(new TestAsyncEnumerable<Client>(new List<Client>
                {
                    new Client
                    {
                        ClientId = clientId,
                        Age = 17,
                        Dialogues = new List<Dialogue>
                        {
                            new Dialogue
                            {
                                DialogueId = Guid.NewGuid(),
                                BegTime = DateTime.Now.AddMinutes(-20),
                                EndTime = DateTime.Now.AddMinutes(-15),
                                StatusId = 3
                            }
                        },
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        Email = "Test@heedbook.com",
                        Name = "TestClient",
                        Gender = "male",
                        Phone = "88008888888",
                        StatusId = 3,
                        ClientNotes = new List<ClientNote>
                        {
                            new ClientNote
                            {
                                ClientNoteId = Guid.NewGuid(),
                                ApplicationUserId = userId,
                                ApplicationUser = new ApplicationUser
                                {
                                    FullName = "TestUser"
                                },
                                Text = "Client like red colour",
                                ClientId = clientId,
                                CreationDate = DateTime.Now.AddDays(-2) 
                            }
                        }
                    }
                }.AsQueryable()));

            //Act
            var result = await clientService.Delete(clientId);
            System.Console.WriteLine(result);

            //Assert            
            Assert.IsTrue(result == "Deleted");
        }
        [Test]
        public async Task ClientNoteGetAllTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.FindOrExceptionOneByConditionAsync<Client>(It.IsAny<Expression<Func<Client, bool>>>()))
                .Returns(Task.FromResult<Client>(
                    new Client
                    {
                        ClientId = clientId,
                        Age = 17,
                        Dialogues = new List<Dialogue>
                        {
                            new Dialogue
                            {
                                DialogueId = Guid.NewGuid(),
                                BegTime = DateTime.Now.AddMinutes(-20),
                                EndTime = DateTime.Now.AddMinutes(-15),
                                StatusId = 3
                            }
                        },
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        Email = "Test@heedbook.com",
                        Name = "TestClient",
                        Gender = "male",
                        Phone = "88008888888",
                        StatusId = 3,
                        ClientNotes = new List<ClientNote>
                        {
                            new ClientNote
                            {
                                ClientNoteId = Guid.NewGuid(),
                                ApplicationUserId = userId,
                                ApplicationUser = new ApplicationUser
                                {
                                    FullName = "TestUser"
                                },
                                Text = "Client like red colour",
                                ClientId = clientId,
                                CreationDate = DateTime.Now.AddDays(-2) 
                            }
                        }
                    }));
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.GetAsQueryable<ClientNote>())
                .Returns(new TestAsyncEnumerable<ClientNote>(new List<ClientNote>
                {
                    
                    new ClientNote
                    {
                        ClientNoteId = Guid.NewGuid(),
                        ApplicationUserId = userId,
                        ApplicationUser = new ApplicationUser
                        {
                            FullName = "TestUser"
                        },
                        Text = "Client like red colour",
                        ClientId = clientId,
                        CreationDate = DateTime.Now.AddDays(-2) 
                    }
                    
                }.AsQueryable()));

            //Act
            var clientNotes = await clientNoteService.GetAll(clientId);

            //Assert            
            Assert.IsTrue(clientNotes.Count > 0);
        }
        [Test]
        public async Task ClientNoteCreateTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentUserId())
                .Returns(userId);
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.GetAsQueryable<Client>())
                .Returns(new TestAsyncEnumerable<Client>(new List<Client>
                {
                    new Client
                    {
                        ClientId = clientId,
                        Age = 17,
                        Dialogues = new List<Dialogue>
                        {
                            new Dialogue
                            {
                                DialogueId = Guid.NewGuid(),
                                BegTime = DateTime.Now.AddMinutes(-20),
                                EndTime = DateTime.Now.AddMinutes(-15),
                                StatusId = 3
                            }
                        },
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        Email = "Test@heedbook.com",
                        Name = "TestClient",
                        Gender = "male",
                        Phone = "88008888888",
                        StatusId = 3,
                        ClientNotes = new List<ClientNote>
                        {
                            new ClientNote
                            {
                                ClientNoteId = Guid.NewGuid(),
                                ApplicationUserId = userId,
                                ApplicationUser = new ApplicationUser
                                {
                                    FullName = "TestUser"
                                },
                                Text = "Client like red colour",
                                ClientId = clientId,
                                CreationDate = DateTime.Now.AddDays(-2) 
                            }
                        }
                    }
                }.AsQueryable()));
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.CreateAsync<ClientNote>(It.IsAny<ClientNote>()))
                .Returns(Task.FromResult(0));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));

            //Act
            var model = new PostClientNote
            {
                ClientId = clientId,
                Text = "TestClient new Note"
            };
            var clientNote = await clientNoteService.Create(model);

            //Assert            
            Assert.IsFalse(clientNote is null);
        }
        [Test]
        public async Task ClientNoteUpdatePutTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var clientNoteId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.FindOrExceptionOneByConditionAsync<ClientNote>(It.IsAny<Expression<Func<ClientNote, bool>>>()))
                .Returns(Task.FromResult(
                    new ClientNote
                    {
                        ClientNoteId = clientNoteId,
                        ApplicationUserId = userId,
                        ApplicationUser = new ApplicationUser
                        {
                            FullName = "TestUser"
                        },
                        Text = "Client like red colour",
                        ClientId = clientId,
                        CreationDate = DateTime.Now.AddDays(-2) 
                    }
                ));
            repositoryMock.Setup(p => p.GetAsQueryable<Client>())
                .Returns(new TestAsyncEnumerable<Client>(new List<Client>
                {
                    new Client
                    {
                        ClientId = clientId,
                        Age = 17,
                        Dialogues = new List<Dialogue>
                        {
                            new Dialogue
                            {
                                DialogueId = Guid.NewGuid(),
                                BegTime = DateTime.Now.AddMinutes(-20),
                                EndTime = DateTime.Now.AddMinutes(-15),
                                StatusId = 3
                            }
                        },
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        Email = "Test@heedbook.com",
                        Name = "TestClient",
                        Gender = "male",
                        Phone = "88008888888",
                        StatusId = 3
                    }
                }.AsQueryable()));
            requestFiltersMock.Setup(p => p.IsCompanyBelongToUser(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<String>()))
                .Returns(true);
            repositoryMock.Setup(p => p.Update<ClientNote>(It.IsAny<ClientNote>()));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));

            //Act
            var model = new PutClientNote
            {
                ClientNoteId = clientNoteId,
                Text = "TestClient new Note"
            };
            var clientNote = await clientNoteService.Update(model);

            //Assert            
            Assert.IsFalse(clientNote is null);
        }
        [Test]
        public async Task ClientNotesDeleteDeleteTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var clientNoteId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            moqILoginService.Setup(p => p.GetCurrentCorporationId())
                .Returns(corporationId);
            repositoryMock.Setup(p => p.FindOrExceptionOneByConditionAsync<ClientNote>(It.IsAny<Expression<Func<ClientNote, bool>>>()))
                .Returns(Task.FromResult(
                    new ClientNote
                    {
                        ClientNoteId = clientNoteId,
                        ApplicationUserId = userId,
                        ApplicationUser = new ApplicationUser
                        {
                            FullName = "TestUser"
                        },
                        Text = "Client like red colour",
                        ClientId = clientId,
                        CreationDate = DateTime.Now.AddDays(-2) 
                    }
                ));
            repositoryMock.Setup(p => p.GetAsQueryable<Client>())
                .Returns(new TestAsyncEnumerable<Client>(new List<Client>
                {
                    new Client
                    {
                        ClientId = clientId,
                        Age = 17,
                        Dialogues = new List<Dialogue>
                        {
                            new Dialogue
                            {
                                DialogueId = Guid.NewGuid(),
                                BegTime = DateTime.Now.AddMinutes(-20),
                                EndTime = DateTime.Now.AddMinutes(-15),
                                StatusId = 3
                            }
                        },
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        Email = "Test@heedbook.com",
                        Name = "TestClient",
                        Gender = "male",
                        Phone = "88008888888",
                        StatusId = 3
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.Delete<ClientNote>(It.IsAny<ClientNote>()));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));

            //Act
            var result = await clientNoteService.Delete(clientId);
            
            //Assert            
            Assert.IsTrue(result == "Deleted");
        }
    }
}