SELECT
                    nb.Id,
                    nb.Name,
                    nb.State,
                    nb.Zone,
                    nb.Region,
                    nb.Megazone,
                    nb.Status,
                    CurrentStatus AS TeamStatus,

                    nt.Id AS TransactionId,
                    nt.ChecklistId,
                    cm.ChecklistName,
                    nt.CurrentStatus,
                    nt.Remarks

                FROM NewBranchMaster nb

                LEFT JOIN NewBranchTransaction nt
                    ON nb.Id = nt.Tid

                LEFT JOIN NewBranchChecklistMaster cm
                    ON nt.ChecklistId = cm.Id

                WHERE
                    ('Pending' IS NULL OR nt.CurrentStatus = 'Pending')
                    AND ('Admin' IS NULL OR cm.Team = 'Admin')
                    AND nt.CurrentStatus = 'Pending'

                ORDER BY nb.Id