SELECT
    cd.Id,
    c1.ChecklistName AS Checklist,
    c1.Team AS ChecklistTeam,

    c2.ChecklistName AS DependsOn,
    c2.Team AS DependsOnTeam

FROM ChecklistDependency cd

INNER JOIN NewBranchChecklistMaster c1
    ON cd.ChecklistId = c1.Id

INNER JOIN NewBranchChecklistMaster c2
    ON cd.DependsOnChecklistId = c2.Id

ORDER BY cd.ChecklistId;