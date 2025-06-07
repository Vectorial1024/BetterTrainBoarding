# BetterTrainBoarding
Unlock the peak efficiency of trains (and also metros) by fixing boarding behavior:

- Passengers board trains (and metros) sensibly by each preferring the closest compartment
  - To be explained below
- Passengers board trains with some respect to the "wait counter" (aka "first come first served")
  - To be explained below

# Mod Status
- No known compatibility problems
- Absolutely save-game compatible: determining which CIM boards which trailer is an instantaneous operation

Because this mod is very small and simple, I am open-sourcing this to everyone out there.

## Types of Transportation Covered
These transportation types are currently covered by this mod:
- Anything under `PassengerTrainAI`:
  - Trains
  - Metros
- Trams
- Buses/Trolleybuses
- Airplanes
- Blimps
- Helicopters

Note: While not directly tested and unprovable from static analysis, monorails should also be covered as well. Do notify me if I am wrong.

## Motivation

This mod aims to salvage vanilla CSL trains and metros from being an ineffective transport option to a reliable transport option. Supposedly, when buses and trams face throughput problems even when Express Bus Services (another mod) is used, then it is time to reorganize them into a high-throughput train/metro line.

I noticed how, for example, the metro becomes unreliable when a lot of passengers are waiting for it. Upon closer look, this is because the game is loading metros in an inefficient way, and in turn, metros spend too much time at stations simply to load passengers. This is unacceptable!

## Technical Information
There are gifs at the `Media` directory to show the behavior difference between the base game and this mod.

We will now explain how this mod works in terms of the algorithm.

### The base game
Train/metro begins loading passengers:
```
For each waiting passenger, from the front of the platform to the end of the platform:
    Find closest free trailer for the passenger
    If such trailer exists:
        Passenger enters that trailer
    Else:
        Entire train is full; stop this operation
```

This results in a negative feedback loop when there are so many passengers at a platform that the train/metro cannot take them all. Some passengers are left behind, then the next train arrives, then the passengers use the above behavior to board the train, which delays the train. When every station behaves like this, the throughput is greatly reduced. This can only be fully "fixed" only when the "passenger cluster" at the end of the platform is cleared.

Moreover, this way of loading passengers do not take into account the wait-timer of citizens, which makes passengers who waited too long and could not board the train utilize their "pocket car", making the situation worse.

### This mod
Train/metro begins loading passengers:

```
For each waiting passenger, identify their ranked choices of preferred trailer order by distance
(ie, they prefer the trailer that is closest to them, and then the 2nd closest, ...)

Note: # of ranked choices = # of trailers that the train has
For each available rank:
    Sort the ranked choices: passengers that have waited a long time will be prioritized for processing
    Try to satisfy their choice by checking whether the chosen trailer is actually free:
        If free, then passenger enters that trailer
        Else try the choice of the next passenger
```

The negative feedback loop is greatly reduced. When the train/metro cannot load all the passengers at the platform, passengers who cannot board the train/metro are somewhat evenly distributed along the platform, and when there are space available, passengers can board the train/metro intelligently and efficiently. There may still be some passengers who need to move to a compartment far from where they are waiting the train/metro, but this occurence has been greatly reduced. This minimizes delay: the bottleneck then becomes unbunching and track status.

Moreover, this algorithm prioritizes passengers that have waited too long. This reduces usage of "pocket cars" among passengers, which significantly limits the situation from spiraling out of control.

### Beneficial side-effects to outside connections traffic
After an unfortunate coding mistake, the mechanism for outside connection spawning has been revealed for all to witness, which basically confirms the following beneficial side effects of this mod:

- Significantly decreased chances an outside connection vehicle arrives with free space
- More regular intervals for outside connection vehicles
- Therefore, decreased stress on the road/train network

The relevant mechanism is basically this:

```
Each outside connection regularly spawns passengers (out of the scope of this mod)
Each passenger has a wait-timer that slowly goes up (aka the "bored timer")
When the wait-timer goes up to 256 (data type max value), the passenger becomes "bored"
    There is a small window of time where this timer stays at 256 (important!!!)
For outside connections, when a passenger becomes bored, a vehicle is spawned
    With this mod, the game tries to arrange earlier passengers to board the vehicle first
```

With this, the leftover passengers that could not board the previous vehicle are much more likely to wait longer to merge with the later passengers, resulting in fewer but packed vehicles spawning.
