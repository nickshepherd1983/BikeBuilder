global using System.Globalization;
global using BikeBuilder.API.Protos;
global using BikeBuilder.Contracts.Components;
global using BikeBuilder.Web.Dialogs;
global using BikeBuilder.Web.Editors;
global using BikeBuilder.Web.Services;
global using Google.Protobuf.WellKnownTypes;
global using Grpc.Core;
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
global using MudBlazor;
// Google.Protobuf.WellKnownTypes also defines a Type; the editor registry means System.Type
// is the one this project talks about.
global using Type = System.Type;
