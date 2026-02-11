# EstateSurveySystem

Ez a repository egy WPF-alapú, RabbitMQ-kommunikációt használó ingatlan állapot felmérő rendszert tartalmaz. A solution négy projektet foglal magában:

- **EstateSurveyContracts**: protobuf szerződések, DTO-k és mapper.
- **EstateSurveyClient**: ügyfél felület (task létrehozás, beküldés, státusz követés).
- **EstateSurveyCentre**: központi felület (task fogadás, jóváhagyás/elutasítás).
- **EstateSurveyAgent**: felmérő ügynök felület (jóváhagyott taskok kezelése).

## Előfeltételek

- Visual Studio 2022
- .NET 6 SDK
- RabbitMQ futó instance (alapértelmezésben `localhost`)

## Futtatás

1. Nyisd meg az `EstateSurveySystem.sln` solutiont.
2. Állítsd be a kívánt startup projektet (Client, Centre, Agent).
3. Indítsd el mindhárom alkalmazást.

## Üzenet routing

- Exchange: `survey.events` (topic)
- Routing key példák:
  - `survey.task.submitted`
  - `survey.task.approved`
  - `survey.task.rejected`
  - `survey.task.agent.inprogress`
  - `survey.task.agent.completed`

## JSON perzisztencia

A projektek a futtatható állomány mellett tárolják a taskokat:

- Client: `client-tasks.json`
- Centre: `centre-tasks.json`
- Agent: `agent-tasks.json`
