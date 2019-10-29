using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using Tests.Models;
using Tests.TestedClass;
using System;

namespace Tests
{
    public class UnitTest2
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void newStrObjSumStringsTest()
        {
            //Arrange
            var mock = new Mock<IRepository>();                 //Привязали объект мок к интерфейсу IRepository. Мок будет имитировать репозиторий
            mock.Setup(p => p.GetAll()).Returns(GetNames());    //с помощью метода Setup устанавливаем привязку, имитируем метод GetAll репозитория методом GetNames
            var newStrObj = new NewString(mock.Object);         //Объявили новый оьъект типа NewString и передали в него объект мок
            
            //Act
            var result = newStrObj.SumStrings();

            //Assert
            Assert.IsNotNull(result);
        }
        private List<string> GetNames()                         //Метод который будет вызываться вместо вызова p.GetAll()
        {
            var names = new List<string>
            {
                "Oleg",
                "vadim",
                "Kirill"
            };
            return names;                                       //Возвращаем список имен
        }
        
    }
}