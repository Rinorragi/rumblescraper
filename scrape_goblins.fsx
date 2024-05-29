// Imports
open System.IO
open System.Net.Http
open System.Net.Http.Json
open FSharp.Data

// Arguments for script, skip the filneme
let args : string array = fsi.CommandLineArgs |> Array.tail
let guildIdParam = args[0]
let tabSeparator = '\t' // Used for copypaste, might be \t or ; or ,
let dataPath = Path.Combine(".","data")
let guildFilePath = Path.Combine(dataPath, (sprintf "%s_goblins.txt" guildIdParam))
let leadersFilePath = Path.Combine(dataPath, (sprintf "%s_leaders.txt" guildIdParam))

// Full models, but we wont use everything
type GuildGoblin = {
    id : int
    name : string
    btag : string
    skulls : int
    honor : int
    avatar : string
    lastUpdate : int64
}

type Guild = {
    id : int
    name : string 
    member_count : int
    description : string
    members : GuildGoblin list
}

type GuildData = {
    data : Guild
    token : string 
    link : string
}

type Goblin = {
    id : int
    name : string
    btag : string
    skulls : int
    honor : int
    level : int 
    pvpWins : int
    leaderAHonor : int
    leaderBHonor : int
    leaderCHonor : int
    armies : string list list 
}

type GoblinData = {
    data : Goblin
    token : string 
}

let ensureDataDirectory = 
    Directory.CreateDirectory(dataPath) |> ignore

#nowarn "3511" // Disable whining about dynamic compilation
let fetchGoblinGuildData (guildId : int) = 
    task {
        use client = new HttpClient()
        let url = $"https://api.warcraftrumble.gg/guild/{guildId}"
        let! guildResult = client.GetFromJsonAsync<GuildData>(url)
        return guildResult
    }

let fetchGoblinData (goblinId : int) = 
    task {
        use client = new HttpClient()
        let url = $"https://api.warcraftrumble.gg/player/{goblinId}"
        let! goblinResult = client.GetFromJsonAsync<GoblinData>(url) 
        return goblinResult
    }

let updateGoblinData = 
    // Fetch guild info
    let guildData = 
        fetchGoblinGuildData (int guildIdParam)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    
    // Fetch goblin info per each guild member
    let goblinData = 
        guildData.data.members
        |> List.map(fun (gg : GuildGoblin) -> fetchGoblinData(gg.id) |> Async.AwaitTask)
        |> Async.Parallel
        |> Async.RunSynchronously
        |> List.ofArray
        |> List.map (fun f -> f.data) // goblins
    goblinData

let persistGoblinStats (goblins : Goblin list) =
    let sgoblins = 
        goblins
        |> List.sortBy (fun (gobo : Goblin) -> gobo.name.ToLower()) // Sort by lowercase because of reasons 
        |> List.fold (fun (allGoblinsString : string) (gobo : Goblin) ->
            let honor = gobo.leaderAHonor + gobo.leaderBHonor + gobo.leaderCHonor
            let goboString = sprintf "%s%c%d%c%d%c%d" gobo.name tabSeparator gobo.skulls tabSeparator honor tabSeparator gobo.level
            sprintf "%s%s%s" allGoblinsString goboString System.Environment.NewLine
        ) ""
    let file = File.CreateText(guildFilePath)
    file.Write(sgoblins.ToCharArray())
    file.Close()

    printfn "%s" guildFilePath

let persistGoblinLeaderStats (goblinData : Goblin list) =
    let goblinLeaders = 
        goblinData
        |> List.map (fun goblin -> 
            let goblinLeaders : (string * int) list =
                goblin.armies 
                |> List.map List.head // First line in armylist is the leader
                |> List.map (fun sLeader -> 
                    let sArr = sLeader.Split(';') 
                    sArr[0], sArr[2] |> int) // leadername - level tuple
            goblin.name, goblinLeaders)
    let allLeaders = 
        goblinLeaders
        |> List.map snd // only leaders
        |> List.concat // all as a long list
        |> List.map fst // only leader name
        |> List.distinct // only unique names
        |> List.sort // alphabetically

    let leaderStats = 
        goblinLeaders
        |> List.map (fun ((goblinName : string), leaderList) -> 
            let leaderDictionary = leaderList |> dict
            let leadersString = 
                allLeaders
                |> List.fold (fun (sLeaders : string) (aLeader : string) -> 
                    let leaderLevel = 
                        match leaderDictionary.ContainsKey(aLeader) with
                        | true -> leaderDictionary[aLeader]
                        | false -> -1
                    sprintf "%s%d%c" sLeaders leaderLevel tabSeparator
                ) ""
            sprintf "%s%c%s%s" goblinName tabSeparator leadersString System.Environment.NewLine)
        |> List.fold (+) ""
    let leaderHeaders = (allLeaders 
        |> List.map (fun sLeader -> sprintf "%s%c" sLeader tabSeparator) 
        |> List.fold (+) "")
    let statsWithHeader : string = sprintf "Goblin%c%s%s%s" tabSeparator leaderHeaders System.Environment.NewLine leaderStats

    let file = File.CreateText(leadersFilePath)
    file.Write(statsWithHeader.ToCharArray())
    file.Close()

    printfn "%s" leadersFilePath

// Create data directory if it does not exists
ensureDataDirectory
// Fetch goblin data from internets to directory
let (goblinDataList : Goblin list) = updateGoblinData 
persistGoblinStats goblinDataList
persistGoblinLeaderStats goblinDataList

