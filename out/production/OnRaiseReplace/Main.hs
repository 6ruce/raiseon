module Main where

import System.Environment
import Data.List.Split
import Data.List
import Data.Maybe
import Data.Either
import Data.Foldable (foldrM)
import Data.Text     (replace, pack, unpack)

data FilePath = FilePath String
    deriving (Show)

-- Line endings
data LineEnding = N | NR

instance Show LineEnding where
    show N  = "\n"
    show NR = "\n\r"

data FileSubPath = FileSubPath String
    deriving (Show)

data LineNumber  = LineNumber Int
    deriving (Show)

data ReplacePlace = ReplacePlace FileSubPath LineNumber
    deriving (Show)

data Result a e = OK a | Err e
    deriving (Show)

data CreatePlaceErr = EmptyLine
                    | WrongLineFormat String
                    | SubPathNotFound

main :: IO ()
main = do
    args <- getArgs
    let filePath       = args !! 0
    let solutionFolder = args !! 1
    let toLineEnding = \ending -> case ending of
         "\n"     -> return N
         "\n\r"   -> return NR
         _        -> fail "Unknown line ending"
    lineEnding <- if length args > 2 then toLineEnding $ args !! 2 else return N
    replaceAll filePath solutionFolder lineEnding

replaceAll :: String -> String -> LineEnding -> IO ()
replaceAll csvFile solutionFolder lineEnding = do
    putStrLn $ "File name: "       ++ csvFile
    putStrLn $ "Solution folder: " ++ solutionFolder
    contents <- readFile csvFile
    let places = getReplacePlaces contents lineEnding
    let (wrongPlaces, correctPlaces) = partitionEithers places
    putStrLn "Analyzing .csv file..."
    printAnalyzingInfo wrongPlaces
    replaceInFiles correctPlaces solutionFolder
    return ()

printAnalyzingInfo :: [String] -> IO ()
printAnalyzingInfo [] = return ()
printAnalyzingInfo xs =
    let countErrorMessages = map (\l -> (length l, head l)) $ group . sort $ xs
    in foldrM (\err _ -> putStrLn $ (show . fst $ err) ++ " errors of '" ++ snd err ++ "'") () countErrorMessages

replaceInFiles :: [ReplacePlace] -> String -> IO ()
replaceInFiles [] _               = return ()
replaceInFiles places solutionDir =
    foldrM (\(ReplacePlace (FileSubPath subPath) (LineNumber number)) _ -> do
        putStrLn $ "-> " ++ subPath ++ " processing"
        replaceInFile (Main.FilePath $ solutionDir ++ "\\" ++ subPath) number) () places

replaceInFile :: Main.FilePath -> Int -> IO ()
replaceInFile (FilePath filePath) lineNumber = do
    contents <- readFile filePath
    let fileLines = lines contents
    let combined = zip [1 .. length fileLines] fileLines
    let replacedContents = map (\(index, line) ->
            if index == lineNumber then unpack . (replace (pack "On") (pack "Raise")) $ (pack line) else line) combined
    length contents `seq` (writeFile filePath $ unlines replacedContents)

getReplacePlaces :: String -> LineEnding -> [Either String ReplacePlace]
getReplacePlaces "" _                    = []
getReplacePlaces fileContents lineEnding =
    let lines = splitOn (show lineEnding) fileContents
    in
        map (toEither . createReplacePlace . (splitOn ";")) lines
        where toEither (OK rp)                      = Right rp
              toEither (Err EmptyLine)              = Left "Empty line not processed"
              toEither (Err (WrongLineFormat line)) = Left $ "Line '" ++ line ++ "' is in wrong format"
              toEither (Err SubPathNotFound)        = Left "TSBpm subpath not found"

createReplacePlace :: [String] -> Result ReplacePlace CreatePlaceErr
createReplacePlace []   = Err EmptyLine
createReplacePlace [""] = Err EmptyLine
createReplacePlace (_ : _ : _ : filePath : lineNumberStr : xs) =
    case split (startsWith "TSBpm") filePath of
        (_ : fileSupPath : xs) ->
            let lineNumber = read $ lineNumberStr :: Int
            in OK $ ReplacePlace (FileSubPath fileSupPath) (LineNumber lineNumber)
        _ -> Err SubPathNotFound
createReplacePlace list = Err $ WrongLineFormat $ intercalate ";" list
