<?php
    include('stormancer.class.php');
    $stormancer = new \stormancer\Stormancer("43a32f08-d286-452f-ac8a-f3e84ce992bc", "gladostesting", "C4EC0D65AC8E23D3D49112758E48567F41204CB5ABAB34354B736B246B0B4976");
    $msg = '';
    $status;
    
    if(!empty($_POST) && $_POST['action'] != 'refresh')
    {
   
            $stormancer->send('session1','action',$_POST["action"]);
    
    
    }
    try
    {
     $status = $stormancer->send('session1','getGameState',$msg);
    }
    catch(Exception $ex)
    {
        var_dump($ex);
    }
?>

<!DOCTYPE html>
<html lang="en">
    <head>
        <meta charset="utf-8" />
        <title></title>
    </head>
    <body>
        <h1>Administration</h1>

        <h2>Informations sur la partie</h2>
        <pre>



<?php 
 var_dump($status)?>
        </pre>
        <p></p>
        <form action="" method="post">
            <input type="submit" name="action" value="start" />
            <input type="submit" name="action" value="stop" />
            <input type="submit" name="action" value="refresh"/>
        </form>

    </body>
</html>
