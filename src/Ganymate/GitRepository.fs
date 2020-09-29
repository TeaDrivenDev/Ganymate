namespace Ganymate

open System

open LibGit2Sharp

module GitRepository =
    type FileChangeKind = Added | Modified | Deleted

    type FileChange =
        {
            Path: string
            ChangeKind: FileChangeKind
        }

    type Committer =
        {
            Name: string
            Email: string
            Date: DateTimeOffset
        }

    type Commit =
        {
            Sha: string
            Author: Committer
            Committer: Committer
            MessageShort: string
            Files: FileChange list
            Diff: string
        }

    //type Repository(repository: LibGit2Sharp.Repository) =
    //    class end

    let isGitRepository directory =
        LibGit2Sharp.Repository.IsValid directory

    let getHead directory =
        use repository = new LibGit2Sharp.Repository(directory)

        repository.Head.FriendlyName

    let createDiffOfTrees (repository: Repository) (compareOptions: CompareOptions) (oldTree: Tree) (newTree: Tree) =
        repository.Diff.Compare<TreeChanges>(oldTree, newTree, compareOptions)
        |> Seq.filter (fun (change: TreeEntryChanges) -> change.Status <> ChangeKind.Unmodified)
        |> Seq.map (fun change -> change.Path, change.Status)

    let getLastCommitChanges directory =
        use repository = new LibGit2Sharp.Repository(directory)

        let last :: previous :: _ =
            repository.Head.Commits
            |> Seq.toList

        let x = 1

        createDiffOfTrees repository (CompareOptions()) previous.Tree last.Tree

    let getLastCommitDiff directory =
        use repository = new LibGit2Sharp.Repository(directory)

        let last :: previous :: _ =
            repository.Head.Commits
            |> Seq.toList

        let patch = repository.Diff.Compare<Patch>(previous.Tree, last.Tree)

        patch.Content

    let createCommitter (signature: Signature) =
        {
            Name = signature.Name
            Email = signature.Email
            Date = signature.When
        }

    let readCommits directory =
        use repository = new Repository(directory)

        repository.Head.Commits
        |> Seq.map (fun commit ->
            {
                Sha = commit.Sha
                Author = createCommitter commit.Author
                Committer = createCommitter commit.Committer
                MessageShort = commit.MessageShort
                Files = []
                    //commit.Parents
                    //|> Seq.tryHead
                    //|> Option.map (fun parent ->
                    //    )
                Diff = ""
            })
        |> Seq.toList

//    let createDiffOfMergedTrees (compareOptions: CompareOptions) (tree: Tree) (parents: Commit seq) (repository: Repository) =
//        repository.Diff.Compare(oldTree, newTree, compareOptions)
//        |> Seq.filter (fun (change: TreeEntryChanges) -> change.Status = ChangeKind.Unmodified)

//private IEnumerable<TreeEntryChanges> GetDiffOfMergedTrees(Repository gitRepo, IEnumerable<LibGit2Sharp.Commit> parents, Tree tree, CompareOptions compareOptions)
//{
//        var firstParent = parents.ElementAt(0);
//        var secondParent = parents.ElementAt(1);

//        var firstChanges = GetDiffOfTrees(gitRepo, firstParent.Tree, tree, compareOptions);
//        var secondChanges = GetDiffOfTrees(gitRepo, secondParent.Tree, tree, compareOptions);

//        var changes = firstChanges.Where(c1 => secondChanges.Any(c2 => c2.Oid == c1.Oid));

//        return changes;
//}

    let getCommits directory =
        use repository = new LibGit2Sharp.Repository(directory)
        repository.Head.Commits |> Seq.toList