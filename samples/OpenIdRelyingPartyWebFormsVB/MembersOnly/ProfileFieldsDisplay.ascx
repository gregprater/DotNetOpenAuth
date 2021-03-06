﻿<%@ Control Language="VB" AutoEventWireup="true" %>
<%@ Import Namespace="DotNetOpenAuth.OpenId.Extensions.SimpleRegistration" %>

<script runat="server">
	
	Private _profileValues As ClaimsResponse
	Public Property ProfileValues() As ClaimsResponse
		Get
			Return _profileValues
		End Get
		Set(ByVal value As ClaimsResponse)
			_profileValues = value
		End Set
	End Property

</script>

<table id="profileFieldsTable" runat="server">
	<tr>
		<td>
			Nickname
		</td>
		<td>
			<%=ProfileValues.Nickname %>
		</td>
	</tr>
	<tr>
		<td>
			Email
		</td>
		<td>
			<%=ProfileValues.Email%>
		</td>
	</tr>
	<tr>
		<td>
			FullName
		</td>
		<td>
			<%=ProfileValues.FullName%>
		</td>
	</tr>
	<tr>
		<td>
			Date of Birth
		</td>
		<td>
			<%=ProfileValues.BirthDate.ToString()%>
		</td>
	</tr>
	<tr>
		<td>
			Gender
		</td>
		<td>
			<%=ProfileValues.Gender.ToString()%>
		</td>
	</tr>
	<tr>
		<td>
			Post Code
		</td>
		<td>
			<%=ProfileValues.PostalCode%>
		</td>
	</tr>
	<tr>
		<td>
			Country
		</td>
		<td>
			<%=ProfileValues.Country%>
		</td>
	</tr>
	<tr>
		<td>
			Language
		</td>
		<td>
			<%=ProfileValues.Language%>
		</td>
	</tr>
	<tr>
		<td>
			Timezone
		</td>
		<td>
			<%=ProfileValues.TimeZone%>
		</td>
	</tr>
</table>
