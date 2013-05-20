<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE xsl:stylesheet [ <!ENTITY nbsp "&#x00A0;"> ]>
<xsl:stylesheet 
  version="1.0" 
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
  xmlns:msxml="urn:schemas-microsoft-com:xslt"
  xmlns:umbraco.library="urn:umbraco.library" 
  xmlns:fulltextsearch.search="urn:fulltextsearch.search"
  xmlns:fulltextsearch.helper="urn:fulltextsearch.helper"
  exclude-result-prefixes="msxml umbraco.library fulltextsearch.search fulltextsearch.helper">
<!--
                            FullTextSearch.xslt
    ======================================================================
                            Full Text Search 
                                  V0.25
    ======================================================================
    
    This XSLT file sets up queries and sends them off to FullTextSearch's 
    XSLT helpers.
    
    Feel free to modify any part of it to your own needs. HTML is near
    the bottom in a couple of templates. 
    
    PARAMETERS:
    
    queryType 
    
    Type of search to perform. Possible values are:
    
    MultiRelevance ->
      The default.
      The index is searched for, in order of decreasing relevance
        1) the exact phrase entered in any of the title properties
        2) any of the terms entered in any of the title properties
        3) a fuzzy match for any of the terms entered in any of the title properties
        4) the exact phrase entered in any of the body properties
        5) any of the terms entered in any of the body properties
        6) a fuzzy match for any of the terms entered in any of the body properties
    
    MultiAnd ->
      Similar to MultiRelevance, but requires all terms be present
    
    SimpleOr->
      Similar to MultiRelevance again, but the exact phrase does not
      get boosted, we just search for any term
      
    AsEntered->
      Search for the exact phrase entered, if more than one term is present
    ______________________________________________________________________    
    
    titleProperties
    
    A comma separated list of properties that are part of the page title,
    these will have their relevance boosted by a factor of 10
    defaults to nodeName. Set to "ignore" not to search titles.
    ______________________________________________________________________    
    
    bodyProperties 
    
    A comma separtated list of properties that are part of the page body.
    These properties and the titleProperties will be searched. 
    
    defaults to using the full text index only
    ______________________________________________________________________
    
    summaryProperties
    
    The list of properties, comma separated, in order of preference,
    that you wish to use to create the summary to appear under
    the title. All properties selected must be in the index, cos that's
    where we pull the data from.
    
    Defaults to Full Text
    ______________________________________________________________________    
    titleLinkProperties
    
    The list of properties, comma separated, in order of preference,that you 
    wish to use to create the title link for each search result. 
    
    Defaults to titleProperties, or if that isn't set nodeName
    ______________________________________________________________________            
    rootNodes
    
    Comma separated list of root node ids
    Only nodes which have one of these nodes as a parent will be returned.
    Default is to search all nodes
    ______________________________________________________________________
    
    contextHighlighting
    
    Set this to false to disable context highlighting
    in the summary/title. You may wish to do this if you are having
    performance issues as context highlighting is (relatively)
    slow.
    Defaults to on.
    
    ______________________________________________________________________
    
    summaryLength
    
    The maximum number of characters to show in the summary.
    Defaults to 300
    
    ______________________________________________________________________
    
    pageLength
    
    Number of results on a page. Defaults to 20. Set to zero to disable paging.
    
    ______________________________________________________________________

    fuzzyness
    
    Lucene Queries can be "fuzzy" or exact.
    A fuzzy query will match close variations of the search terms, such as 
    plurals etc. This sets how close the search term must be to a term in
    the index. Values from zero to one. 1.0 = exact matching.
    Note that fuzzy matching is slow compared to exact or even wildcard
    matching, if you're having performance issues this is the first thing
    to switch off.
    
    Defaults to 0.8
    ______________________________________________________________________
    
    useWildcards
    
    Add a wildcard "*" to the end of every search term to make it match 
    anything starting with the search term. This is a slightly faster, but
    less accurate way of achieving the same ends as fuzzy matching. 
    Note that fuzzyness is automatically set to 1.0 if a wildcards are enabled.
    
    Defaults to off
    ______________________________________________________________________
-->

<xsl:output method="xml" omit-xml-declaration="yes"/>
<xsl:param name="currentPage"/>

<!-- Script Variables -->
    
<!-- 
    full text index name. Do not change this without good reason. 
-->
<xsl:variable name="fullTextIndexName">FullTextSearch</xsl:variable>

<!--
    Name of the GET/POST parameter that contains the search terms
-->
<xsl:variable name="getPostTerms">Search</xsl:variable>
    
<!--
    Name of the GET/POST parameter that contains the page number
-->
<xsl:variable name="getPostPage">Page</xsl:variable>

<!--
    Number of pages to show in pagination pager
-->
<xsl:variable name="numNumbers">15</xsl:variable>

<!-- Parameters from Macro-->

<xsl:variable name="titleProperties">
  <xsl:choose>
    <xsl:when test="string(/macro/titleProperties)='ignore'"></xsl:when>
    <xsl:when test="string(/macro/titleProperties)">
      <xsl:value-of select="/macro/titleProperties" />
    </xsl:when>
    <xsl:otherwise>nodeName</xsl:otherwise>
  </xsl:choose>
</xsl:variable>

<xsl:variable name="bodyProperties">
  <xsl:choose>
    <xsl:when test="string(/macro/bodyProperties)">
      <xsl:value-of select="/macro/bodyProperties" />
    </xsl:when>
    <xsl:otherwise>
      <xsl:value-of select="$fullTextIndexName" />
    </xsl:otherwise>
  </xsl:choose>
</xsl:variable>
    
<xsl:variable name="summaryProperties">
  <xsl:choose>
    <xsl:when test="string(/macro/summaryProperties)">
      <xsl:value-of select="/macro/summaryProperties" />
    </xsl:when>
    <xsl:otherwise>
      <xsl:value-of select="$fullTextIndexName" />
    </xsl:otherwise>
  </xsl:choose>
</xsl:variable>

<xsl:variable name="titleLinkProperties">
  <xsl:choose>
    <xsl:when test="string(/macro/titleLinkProperties)">
      <xsl:value-of select="/macro/titleLinkProperties" />
    </xsl:when>
    <xsl:when test="string($titleProperties)">
      <xsl:value-of select="$titleProperties" />
    </xsl:when>
    <xsl:otherwise>nodeName</xsl:otherwise>
  </xsl:choose>
</xsl:variable>
<xsl:variable name="rootNodes" select="/macro/rootNodes" />

<xsl:variable name="contextHighlighting">
  <xsl:choose>
    <xsl:when test="number(/macro/contextHighlighting)=0">0</xsl:when>
    <xsl:otherwise>1</xsl:otherwise>
  </xsl:choose>
</xsl:variable>

<xsl:variable name="summaryLength">
  <xsl:choose>
    <xsl:when test="number(/macro/summaryLength) &gt; 0">
      <xsl:value-of select="number(/macro/summaryLength)" />
    </xsl:when>
    <xsl:otherwise>300</xsl:otherwise>
  </xsl:choose>
</xsl:variable>

  
<xsl:variable name="pageLength">
  <xsl:choose>
    <xsl:when test="number(/macro/pageLength)">
      <xsl:value-of select="number(/macro/pageLength)" />
    </xsl:when>
    <xsl:otherwise>20</xsl:otherwise>
  </xsl:choose>
</xsl:variable>
  
<xsl:variable name="queryType" >
  <xsl:choose>
    <xsl:when test="string(/macro/queryType)">
      <xsl:value-of select="/macro/queryType" />
    </xsl:when>
    <xsl:otherwise>
      <xsl:value-of select="MultiRelevance" />
    </xsl:otherwise>
  </xsl:choose>
</xsl:variable>

<xsl:variable name="fuzzyness">
  <xsl:choose>
    <xsl:when test="string(/macro/fuzzyness)">
      <xsl:value-of select="/macro/fuzzyness" />
    </xsl:when>
    <xsl:otherwise>0.8</xsl:otherwise>
  </xsl:choose>
</xsl:variable>
    
<xsl:variable name="useWildcards">
  <xsl:choose>
    <xsl:when test="number(/macro/useWildcards)=1">1</xsl:when>
    <xsl:otherwise>0</xsl:otherwise>
  </xsl:choose>
</xsl:variable>

<!-- Call XSLT helpers to query index and process results -->
<xsl:variable name="pageNumber">
  <xsl:choose>
    <xsl:when test="number(umbraco.library:RequestQueryString($getPostPage)) &gt; 0">
      <xsl:value-of select="number(umbraco.library:RequestQueryString($getPostPage))" />
    </xsl:when>
    <xsl:when test="number(umbraco.library:Request($getPostPage)) &gt; 0">
      <xsl:value-of select="number(umbraco.library:Request($getPostPage))" />
    </xsl:when>
    <xsl:otherwise>1</xsl:otherwise>
  </xsl:choose>
</xsl:variable>

<xsl:variable name="searchTerms">
  <xsl:choose>
    <xsl:when test="string(umbraco.library:RequestQueryString($getPostTerms))">
      <xsl:value-of select="umbraco.library:RequestQueryString($getPostTerms)" />
    </xsl:when>
    <xsl:when test="string(umbraco.library:Request($getPostTerms))">
      <xsl:value-of select="umbraco.library:Request($getPostTerms)" />
    </xsl:when>
    <xsl:otherwise></xsl:otherwise>
  </xsl:choose>
</xsl:variable>

<xsl:variable name="searchTermsUrlEncoded" select="umbraco.library:UrlEncode($searchTerms)" />

<!--
    Language entries used multiple times
-->
    <xsl:variable name="langPrevious" select="fulltextsearch.helper:DictionaryHelper('NavPrevious')"/>
    <xsl:variable name="langNext" select="fulltextsearch.helper:DictionaryHelper('NavNext')"/>
<!-- 
  Here the surprisingly named XSLT Helper function "Search" is called to 
  do the actual search, and return the results as XML.
-->
<xsl:variable name="results" select="fulltextsearch.search:Search($queryType,$searchTerms,$titleProperties,$bodyProperties,$rootNodes,$titleLinkProperties,$summaryProperties,$contextHighlighting,$summaryLength,$pageNumber,$pageLength,$fuzzyness,$useWildcards)"  />
  
<xsl:template match="/">
  <!-- Main Template, Check for results, display errors -->
  <div class="fulltextsearch">
    <!-- search box -->
    <div class="fulltextsearch_searchboxcontainer">
      <xsl:call-template name="searchBox" />
    </div>
    <xsl:choose>
      <xsl:when test="$results/error">
        <xsl:call-template name="showErrors">
          <xsl:with-param name="searchErrors" select="$results" />
        </xsl:call-template>
      </xsl:when>
      <xsl:when test="$results/results">
        <xsl:call-template name="showResults">
          <xsl:with-param name="searchResults" select="$results" />
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:call-template name="showErrors" >
          <xsl:with-param name="showErrors" select="$results" />
        </xsl:call-template>
      </xsl:otherwise>
    </xsl:choose>
  </div>
</xsl:template>

<!-- results template. The HTML output for your search results is here -->
<xsl:template name="showResults">
  <xsl:param name="searchResults" />
  <div class="fulltextsearch_results">
    <h4 class="fulltextsearch_results_heading">
        <xsl:value-of select="fulltextsearch.helper:StringFormat(
          fulltextsearch.helper:DictionaryHelper('SearchResultsFor'),
          $searchTerms)" />
    </h4>
    <xsl:if test="number($searchResults/results/summary/@numPages) &gt; 1">
      <xsl:call-template name="pagination">
        <xsl:with-param name="numPages" select="$searchResults/results/summary/@numPages" />
        <xsl:with-param name="pageNumber" select="$pageNumber" />
      </xsl:call-template>
    </xsl:if>
    <xsl:for-each select="$searchResults/results/nodes/*" >
      <div class="fulltextsearch_result">
        <p class="fulltextsearch_title">
          <a class="fulltextsearch_titlelink">
              <xsl:attribute name="href">
                <xsl:value-of select="umbraco.library:NiceUrl(@id)" />
              </xsl:attribute>
            <xsl:value-of select="./data [@alias='FullTextTitle']" disable-output-escaping="yes"/>
          </a>
        </p>
        <p class="fulltextsearch_summary">
          <xsl:value-of select="./data [@alias='FullTextSummary']" disable-output-escaping="yes"/>
        </p>
      </div>
    </xsl:for-each>
    <xsl:if test="number($searchResults/results/summary/@numPages) &gt; 1">
      <xsl:call-template name="pagination">
        <xsl:with-param name="numPages" select="$searchResults/results/summary/@numPages" />
        <xsl:with-param name="pageNumber" select="$pageNumber" />
      </xsl:call-template>
    </xsl:if>
    <p class="fulltextsearch_info">
      <xsl:value-of select="fulltextsearch.helper:StringFormat(fulltextsearch.helper:DictionaryHelper('SummaryInfoFormat'),
                      $searchResults/results/summary/@firstResult,
                      $searchResults/results/summary/@lastResult,
                      $searchResults/results/summary/@numResults,
                      $searchResults/results/summary/@timeTaken
        )" />
        <xsl:if test="$searchResults/results/summary/swinfo">
        <xsl:value-of select="$searchResults/results/summary/swinfo" disable-output-escaping="yes" />
        </xsl:if>
    </p>
  </div>
</xsl:template>
      
      
<!-- errors template. The HTML output on error (no match etc.) is here. -->
<xsl:template name="showErrors">
  <xsl:param name="searchErrors" />
  <xsl:variable name="dictionaryError">
    <xsl:choose>
      <xsl:when test="$searchErrors and $searchErrors/error/@type">
        <xsl:value-of select="fulltextsearch.helper:StringFormat(
                                fulltextsearch.helper:DictionaryHelper($searchErrors/error/@type),
                                $searchTerms,
                                $pageNumber
        )" />
      </xsl:when>
      <xsl:otherwise></xsl:otherwise>
    </xsl:choose>
  </xsl:variable>

  <xsl:variable name="errormsg">
    <xsl:choose>
      <xsl:when test="string($dictionaryError)">
        <xsl:value-of select="string($dictionaryError)" />
      </xsl:when>
      <xsl:when test="$searchErrors and string($searchErrors/error)">
        <xsl:value-of select="string($searchErrors/error)" />
      </xsl:when>
      <xsl:when test="string(fulltextsearch.helper:DictionaryHelper('UnknownError'))">
        <xsl:value-of select="fulltextsearch.helper:DictionaryHelper('UnknownError')" />
      </xsl:when>
      <xsl:otherwise>
        An unknown search error has occurred. Please Check the umbraco error log.
      </xsl:otherwise>
    </xsl:choose>
  </xsl:variable>
  <div class="fulltextsearch_error">
    <p>
      <xsl:value-of select="$errormsg" />
    </p>
  </div>
</xsl:template>

<!-- Pagination -->
<xsl:template name="pagination">
  <xsl:param name="numPages" />
  <xsl:param name="pageNumber" />
  <div class="fulltextsearch_pagination">
    <ul class="fulltextsearch_pagination_ul">
    <xsl:choose>
    <xsl:when test="$pageNumber &gt; 1">
      <li class="fulltextsearch_previous">
        <a class="fulltextsearch_pagination_link">
          <xsl:attribute name="href">
            <xsl:text>?</xsl:text>
            <xsl:value-of select="$getPostTerms" />
            <xsl:text>=</xsl:text>
            <xsl:value-of select="$searchTermsUrlEncoded"  />
            <xsl:text>&amp;</xsl:text>
            <xsl:value-of select="$getPostPage" />
            <xsl:text>=</xsl:text>
            <xsl:value-of select="$pageNumber - 1" />
          </xsl:attribute>
          <xsl:value-of select="$langPrevious"/>
        </a>
      </li>
    </xsl:when>
    <xsl:otherwise>
      <li class="fulltextsearch_previous fulltextsearch_previous_inactive">
        <a class="fulltextsearch_pagination_link">
          <xsl:value-of select="$langPrevious"/>
        </a>
      </li>
    </xsl:otherwise>
    </xsl:choose>
    <xsl:variable name="startPage">
      <xsl:choose>
        <xsl:when test="$pageNumber &lt; (floor($numNumbers div 2)+1)">1</xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$pageNumber - floor($numNumbers div 2)" />
        </xsl:otherwise>
      </xsl:choose>
    </xsl:variable>
    <xsl:call-template name="numbers">
      <xsl:with-param name="numPages" select="$numPages" />
      <xsl:with-param name="startPage" select="$startPage" />
      <xsl:with-param name="thisPage" select="$startPage" />
    </xsl:call-template>
    <xsl:choose>
      <xsl:when test="$pageNumber &lt; $numPages">
        <li class="fulltextsearch_next">
          <a class="fulltextsearch_pagination_link">
            <xsl:attribute name="href">
              <xsl:text>?</xsl:text>
              <xsl:value-of select="$getPostTerms" />
              <xsl:text>=</xsl:text>
              <xsl:value-of select="$searchTermsUrlEncoded" />
              <xsl:text>&amp;</xsl:text>
              <xsl:value-of select="$getPostPage" />
              <xsl:text>=</xsl:text>
              <xsl:value-of select="$pageNumber + 1" />
            </xsl:attribute>
            <xsl:value-of select="$langNext"/>
          </a>
        </li>
      </xsl:when>
      <xsl:otherwise>
        <li class="fulltextsearch_next fulltextsearch_next_inactive">
          <a class="fulltextsearch_pagination_link">
            <xsl:value-of select="$langNext"/>
          </a>
        </li>
      </xsl:otherwise>
    </xsl:choose>
    </ul>
  </div>
</xsl:template>

<xsl:template name="numbers">
  <xsl:param name="numPages" />
  <xsl:param name="startPage" />
  <xsl:param name="thisPage" />
  <xsl:choose>
    <xsl:when test="$thisPage = $pageNumber">
      <li class="fulltextsearch_page fulltextsearch_thispage">
          <xsl:value-of select="$thisPage" />
      </li>
    </xsl:when>
    <xsl:otherwise>
      <li class="fulltextsearch_page">
        <a class="fulltextsearch_pagination_link">
          <xsl:attribute name="href">
            <xsl:text>?</xsl:text>
            <xsl:value-of select="$getPostTerms" />
            <xsl:text>=</xsl:text>
            <xsl:value-of select="$searchTermsUrlEncoded" />
            <xsl:text>&amp;</xsl:text>
            <xsl:value-of select="$getPostPage" />
            <xsl:text>=</xsl:text>
            <xsl:value-of select="$thisPage" />
          </xsl:attribute>
          <xsl:value-of select="$thisPage" />
        </a>
      </li>
    </xsl:otherwise>
  </xsl:choose>
  <xsl:if test="($thisPage &lt; ($startPage + $numNumbers - 1)) and ($thisPage &lt; $numPages)">
    <xsl:call-template name="numbers">
      <xsl:with-param name="numPages" select="$numPages" />
      <xsl:with-param name="startPage" select="$startPage" />
      <xsl:with-param name="thisPage" select="$thisPage + 1" />
    </xsl:call-template>
  </xsl:if>
</xsl:template>
<!-- Search Box -->
<xsl:template name="searchBox">
  <xsl:variable name="searchBox">
    <xsl:choose>
      <xsl:when test="string($searchTerms)">
        <xsl:value-of select="$searchTerms" />
      </xsl:when>
      <xsl:otherwise></xsl:otherwise>
    </xsl:choose>
  </xsl:variable>
    <form class="fulltextsearch_form" method="get">
      <xsl:attribute name="action">
        <xsl:value-of select="umbraco.library:NiceUrl($currentPage/@id)" />
      </xsl:attribute>
      <input class="fulltextsearch_searchbox" name="{$getPostTerms}" type="text" value="{$searchTerms}" />
      <input class="fulltextsearch_searchbutton" type="submit" value="{fulltextsearch.helper:DictionaryHelper('SearchButton')}" />
    </form>
</xsl:template>
</xsl:stylesheet>