$questPath='C:\test\Shrimp\Assets\GameDesign\Data\QuestDatabase_Sample.asset'
$questLines=Get-Content $questPath
$questMap=@{}
$current=$null
$inStages=$false
$stage=$null

foreach($line in $questLines){
    if($line -match '^  - questId: (.+)$'){
        if($null -ne $current){
            if($null -ne $stage){
                $current.stages += $stage
                $stage=$null
            }
            $questMap[$current.id]=$current
        }

        $qid=$matches[1].Trim()
        $current=[ordered]@{ id=$qid; name=''; desc=''; type=''; stages=@() }
        $inStages=$false
        continue
    }

    if($null -eq $current){ continue }

    if($line -match '^    questName: (.*)$'){
        $current.name=$matches[1].Trim()
        continue
    }

    if($line -match '^    description: (.*)$'){
        $current.desc=$matches[1].Trim()
        continue
    }

    if($line -match '^    questType: (.*)$' -and -not $inStages){
        $current.type=$matches[1].Trim()
        continue
    }

    if($line -match '^    stages:$'){
        $inStages=$true
        continue
    }

    if($inStages -and $line -match '^    nextQuestIds:'){
        if($null -ne $stage){
            $current.stages += $stage
            $stage=$null
        }
        $inStages=$false
        continue
    }

    if($inStages){
        if($line -match '^    - stageId: (.*)$'){
            if($null -ne $stage){
                $current.stages += $stage
            }
            $stage=[ordered]@{ id=$matches[1].Trim(); title=''; type=''; target='' }
            continue
        }

        if($null -eq $stage){ continue }

        if($line -match '^      title: (.*)$'){
            $stage.title=$matches[1].Trim()
            continue
        }

        if($line -match '^      questType: (.*)$'){
            $stage.type=$matches[1].Trim()
            continue
        }

        if($line -match '^      targetCount: (.*)$'){
            $tc=$matches[1].Trim()
            if($tc -ne '0'){
                $stage.target = "count=$tc"
            }
            continue
        }

        if($line -match '^      targetStrongholdId: (.*)$'){
            $v=$matches[1].Trim()
            if($v){
                if($stage.target){ $stage.target += ';' }
                $stage.target += "stronghold=$v"
            }
            continue
        }

        if($line -match '^      targetBossId: (.*)$'){
            $v=$matches[1].Trim()
            if($v){
                if($stage.target){ $stage.target += ';' }
                $stage.target += "boss=$v"
            }
            continue
        }

        if($line -match '^      targetWaveEventType: (.*)$'){
            $v=$matches[1].Trim()
            if($v -ne '0'){
                if($stage.target){ $stage.target += ';' }
                $stage.target += "eventType=$v"
            }
            continue
        }
    }
}

if($null -ne $current){
    if($null -ne $stage){ $current.stages += $stage }
    $questMap[$current.id]=$current
}

$levelFiles=Get-ChildItem -Path 'C:\test\Shrimp\Assets\GameDesign\Data' -Filter 'LevelData_Level*.asset' | Sort-Object Name
foreach($file in $levelFiles){
    $lines=Get-Content $file.FullName

    $levelId=((($lines|Select-String '^  levelId: ' | Select-Object -First 1).Line) -split ':',2)[1].Trim()
    $scene=((($lines|Select-String '^  sceneName: ' | Select-Object -First 1).Line) -split ':',2)[1].Trim()
    $power=((($lines|Select-String '^  recommendedPower: ' | Select-Object -First 1).Line) -split ':',2)[1].Trim()

    $bossLine=($lines|Select-String '^  overrideBossSettings: ' | Select-Object -First 1)
    $boss=$false
    if($bossLine){
        $boss=((($bossLine.Line) -split ':',2)[1].Trim() -eq '1')
    }

    $bossName=''
    if($boss){
        $bl=($lines|Select-String '^  bossName: ' | Select-Object -First 1)
        if($bl){
            $bossName=((($bl.Line) -split ':',2)[1].Trim())
        }
    }

    $next=((($lines|Select-String '^  nextLevelId: ' | Select-Object -First 1).Line) -split ':',2)[1].Trim()

    $inQ=$false
    $qEntries=@()
    foreach($line in $lines){
        if($line -match '^  quests:'){ $inQ=$true; continue }
        if($inQ -and $line -match '^  strongholdOverrides:'){ break }

        if($inQ -and $line -match '^  - questId: (.+)$'){
            $qEntries += [ordered]@{ id=$matches[1].Trim(); req=''; order='' }
            continue
        }

        if($inQ -and $qEntries.Count -gt 0 -and $line -match '^    required: (.+)$'){
            $qEntries[$qEntries.Count-1].req=$matches[1].Trim()
            continue
        }

        if($inQ -and $qEntries.Count -gt 0 -and $line -match '^    order: (.+)$'){
            $qEntries[$qEntries.Count-1].order=$matches[1].Trim()
            continue
        }
    }

    $waveCount=(@($lines|Where-Object {$_ -match '^    - waveIndex:'})).Count
    $eventTypes=@()
    foreach($line in $lines){
        if($line -match '^        eventType: (.+)$'){
            $eventTypes += $matches[1].Trim()
        }
    }
    $eventTypeSet=($eventTypes|Select-Object -Unique) -join '/'

    Write-Output "=== $levelId | scene=$scene | recPower=$power | waves=$waveCount | eventTypes=$eventTypeSet | boss=$boss $bossName | next=$next ==="

    foreach($q in $qEntries){
        $qData=$null
        if($questMap.ContainsKey($q.id)){ $qData=$questMap[$q.id] }
        $qName=if($qData){$qData.name}else{'<missing>'}
        $role=if($q.req -eq '1'){'Main'}else{'Side'}
        Write-Output ("  [$role] {0} | {1}" -f $q.id,$qName)
        if($qData -and $qData.stages.Count -gt 0){
            $stageTexts=@()
            foreach($s in $qData.stages){
                $t=if([string]::IsNullOrWhiteSpace($s.target)){''}else{" ({0})" -f $s.target}
                $stageTexts += ("{0}[T{1}]{2}" -f $s.title,$s.type,$t)
            }
            Write-Output ("    stages: " + ($stageTexts -join " -> "))
        }
    }
}
