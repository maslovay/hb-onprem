using System;
using Microsoft.Extensions.DependencyInjection;

namespace TabletLoadTest
{
    public class TabletLoadData
    {
        public string command;
        public Int32 numberOfExtendedDevices;
        public Int32 numberOfNotExtendedDevices;
        public IServiceProvider serviceProvider;
    }
}