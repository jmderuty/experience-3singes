<?php
    /****************************************************
     * Class Stormancer :
     ****************************************************
     * Author  : CLEVE Nicolas, DERUTY Jean-Michel
     * Date    : 05/02/14
     * Version : 0.1
     ****************************************************/
    namespace stormancer;
    
    class Stormancer
    {
        const STORMANCER_URI = "http://api.stormancer.com";
    
        /* Number of seconds your request is valid to connect to stormancer */
        const EXPIRATION_DATE = 30;
       //: CURL Infos
        /* Curl port to send Http (POST/GET/PUT) requests */
        const CURL_PORT = 80;
        /* Number of seconds before CURL timeout */
        const TIMEOUT = 30;
        /* Max number of Scene(s)/Route(s) stacked (before flush) */
        const MAX_STACK_ENTROPY = 5;
    
        /* Max number of Message(s) stacked (before flush) */
        const MAX_STACK_COUNT = 10;
    
        /* File path to log error : if empty, no logs */
        const FILE_ERROR = "stormancer.log";
    
        /* Set PROXY if you need it for requests */
        private static $sPROXY = null; //'127.0.0.1:8888';
        //: Ex. For Fiddler tests use '127.0.0.1:8888'
        //: End configuration
        private $accountId;
        private $secret;
        private $appName;
    
        public $proxy;
        public $retries = 3;
        // Retry interval (ms)
        public $retry_interval = 50;
    
        public function __construct($accountId, $appName,$secret)
        {
           $this->accountId = $accountId;
           $this->secret = trim($secret);
           $this->appName = $appName;
        }
    
        public function getScenes()
        {
            $sUri = self::STORMANCER_URI.'/'.$this->accountId.'/'.$this->appName.'/scenes/';
            $sToken = $this->getToken();
            return json_decode($this->curl_sender($sUri, $sToken,null, 'GET'));
        }
    
        public function createScene($sSceneId,$type,$public, array $tags = array())
        {
            if(! self::checkScene($sSceneId) )
            {
                throw new SceneIdInvalid('SceneId format Invalid : ' . $sSceneId);
            }
    
            $sUri = self::STORMANCER_URI.'/'.urlencode( $this->accountId).'/'.urlencode($this->appName).'/scenes/'.urlencode($sSceneId);
            $sToken = $this->getToken();
            $data = array(
            'Id'=> $sSceneId,
            'Type'=>$type,
            'Tags'=>$tags,
            'LifecycleMode'=>'Transient',
            'Public'=>$public
            );
            $body = json_encode($data);
            return $this->curl_sender_retries($sUri, $sToken,$body, 'PUT');
    
        }
    
        public function deleteScene($sSceneId)
        {
            if(! self::checkScene($sSceneId) )
            {
                throw new SceneIdInvalid('SceneId format Invalid : ' . $sSceneId);
            }
    
            $sUri = self::STORMANCER_URI.'/'.urlencode( $this->accountId).'/'.urlencode($this->appName).'/scenes/'.urlencode($sSceneId);
            $sToken = $this->getToken();
    
            return $this->curl_sender_retries($sUri, $sToken,null, 'DELETE');
        }
    
        public function auth($sSceneId, $aUser)
        {
            if( self::checkScene($sSceneId) ) {
                $sUri = self::STORMANCER_URI.'/'.urlencode( $this->accountId).'/'.urlencode($this->appName).'/scenes/'.urlencode($sSceneId).'/token';
                $sToken = $this->getToken();
    
                $body = json_encode($aUser);
                $sCurlReturned = $this->curl_sender($sUri, $sToken, $body, 'POST');        //: Use Fiddler for debug
    
                return $sCurlReturned;
            } 
            else 
            {
                throw new SceneIdInvalid('SceneId format Invalid : ' . $sSceneId);
            }
        }
    
        public function send($sSceneId, $sRoute,$aArgs)
        {
            if(! self::checkScene($sSceneId) )
            {
                throw new SceneIdInvalid('SceneId format Invalid : ' . $sSceneId);
            }

            $sUri = self::STORMANCER_URI.'/'.urlencode( $this->accountId).'/'.urlencode($this->appName).'/scenes/'.urlencode($sSceneId).'/message/'.urlencode($sRoute);
            $sToken = $this->getToken();
            $body = json_encode($aArgs);
            $result = $this->curl_sender($sUri,$sToken,$body,'POST');
            return json_decode($result);
        }
        private static function checkScene($sSceneId) 
        {
            return preg_match('/^[a-z0-9\-_]+/i', $sSceneId);
        }
    
        /**Create a stormancer token*/
        private function getToken() {
         $timezone = date_default_timezone_get();
            date_default_timezone_set("UTC");
            $fTime = microtime(true) + self::EXPIRATION_DATE;
            $sMicro = strstr($fTime, '.');
            $aToken = array("Expiration" => date("Y-m-d\TH:i:s", $fTime).$sMicro.'Z');
            $sToken = json_encode($aToken);
            $sUTF8 = utf8_encode($sToken);
            $sBase64 = base64_encode($sUTF8);
            $sConcat = $sBase64 . $this->secret;
            $sSHA256 = hash("sha256", $sConcat, true);
            $sSign = base64_encode($sSHA256);
            date_default_timezone_set($timezone);
            return $sBase64 . '.' . $sSign;
        }
        private function curl_sender_retries($sUri, $sToken, $sData, $sType = 'POST')
        {
            $retries = $this->retries;
            while($retries >= 0)
            {
                
                $retries --;
                try
                {
                    
                    $result = $this->curl_sender($sUri, $sToken, $sData,$sType);
                   
                    return $result;
                }
                catch(\Exception $ex)
                {
                    
                    if($retries <0)
                    {
                        
                        throw $ex;
                    }
                }
               
                usleep(1000*$this->retry_interval);
            }
        }
        private function curl_sender($sUri, $sToken, $sData, $sType = 'POST')
        {
            $bHttps = !strncmp($sUri, 'https', strlen('https'));
    
            $bPost = ($sType == 'POST');
            $oCurl = curl_init($sUri);
    
            curl_setopt($oCurl,CURLOPT_CUSTOMREQUEST,$sType);
            //curl_setopt($oCurl, CURLOPT_POST, $bPost);
            curl_setopt($oCurl, CURLOPT_HEADER, false);
            curl_setopt($oCurl, CURLOPT_FRESH_CONNECT, true);
            curl_setopt($oCurl, CURLOPT_RETURNTRANSFER, true);
            curl_setopt($oCurl, CURLOPT_FORBID_REUSE, true);
            curl_setopt($oCurl, CURLOPT_TIMEOUT, self::TIMEOUT);
            if( $bHttps ) {
                curl_setopt($oCurl, CURLOPT_SSL_VERIFYPEER, false);
            }
            if($sData != null)
            {
                curl_setopt($oCurl, CURLOPT_POSTFIELDS, $sData);
            }
            if( !empty($this->proxy) ) curl_setopt($oCurl, CURLOPT_PROXY, self::$sPROXY);
            curl_setopt($oCurl, CURLOPT_HTTPHEADER, array(
                "Content-Type: application/json",
                "Accept: application/json",
                "X-Token: ".$sToken
            ));
    
            $sReturn = curl_exec($oCurl);
    
            $code = curl_getinfo($oCurl,CURLINFO_HTTP_CODE);
            if( $code !=200 ) { 
                $curlErrorMsg = curl_error($oCurl);    
                $msg = '';
                if($code != 0)
                {
                    $msg .='Error Code:'.$code;
                } 
                if($curlErrorMsg != '')
                {
                    $msg .=', Curl Error: '.$curlErrorMsg;
                }
                if($sReturn != '')
                {
                    $msg .=', Message: '.$sReturn;
                }
                throw new \Exception($msg);
            } else return $sReturn;
        }
    }
?>



