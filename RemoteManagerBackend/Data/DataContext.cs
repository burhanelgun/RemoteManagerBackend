﻿using Microsoft.EntityFrameworkCore;
using RemoteManagerBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteManagerBackend.Data
{
    public class DataContext:DbContext
    {
        public DataContext (DbContextOptions<DataContext> options):base(options)
        {

        }

        public DbSet<Manager> Managers { get; set; }
        public DbSet<CreateJob> Jobs { get; set; }

    }
}
