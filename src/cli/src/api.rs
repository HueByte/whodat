use anyhow::{Context, Result};
use serde::{Deserialize, Serialize};
use std::collections::BTreeMap;

const DEFAULT_API: &str = "https://whodat.dev";

pub struct Client {
    base: String,
    http: reqwest::blocking::Client,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct Entry {
    pub handle: String,
    pub text: Option<String>,
    pub avatar_ascii: Option<String>,
    #[serde(default)]
    pub metadata: BTreeMap<String, String>,
    pub registered_at: i64,
    pub updated_at: i64,
}

#[derive(Debug, Serialize)]
pub struct RegisterRequest<'a> {
    pub handle: &'a str,
    pub password: Option<&'a str>,
    pub text: Option<&'a str>,
    pub avatar_ascii: Option<&'a str>,
    pub metadata: Option<&'a BTreeMap<String, String>>,
}

#[derive(Debug, Deserialize)]
pub struct TokenResponse {
    pub token: String,
    pub handle: String,
}

#[derive(Debug, Default, Serialize)]
pub struct UpdateRequest<'a> {
    #[serde(skip_serializing_if = "Option::is_none")]
    pub text: Option<&'a str>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub avatar_ascii: Option<&'a str>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub metadata: Option<&'a BTreeMap<String, String>>,
}

impl Client {
    pub fn from_env(override_url: Option<&str>) -> Result<Self> {
        let base = override_url
            .map(str::to_string)
            .or_else(|| std::env::var("WHODAT_API").ok())
            .unwrap_or_else(|| DEFAULT_API.to_string());

        let http = reqwest::blocking::Client::builder()
            .user_agent(concat!("whodat/", env!("CARGO_PKG_VERSION")))
            .build()
            .context("building HTTP client")?;
        Ok(Self { base, http })
    }

    pub fn lookup(&self, handle: &str) -> Result<Entry> {
        let url = format!("{}/api/u/{}", self.base, handle);
        let resp = self.http.get(url).send()?.error_for_status()?;
        Ok(resp.json()?)
    }

    pub fn me(&self, token: &str) -> Result<Entry> {
        let url = format!("{}/api/u/me", self.base);
        let resp = self
            .http
            .get(url)
            .bearer_auth(token)
            .send()?
            .error_for_status()?;
        Ok(resp.json()?)
    }

    pub fn register(&self, req: &RegisterRequest<'_>) -> Result<TokenResponse> {
        let url = format!("{}/api/register", self.base);
        let resp = self.http.post(url).json(req).send()?.error_for_status()?;
        Ok(resp.json()?)
    }

    pub fn update(&self, token: &str, req: &UpdateRequest<'_>) -> Result<Entry> {
        let url = format!("{}/api/u/me", self.base);
        let resp = self
            .http
            .put(url)
            .bearer_auth(token)
            .json(req)
            .send()?
            .error_for_status()?;
        Ok(resp.json()?)
    }

    pub fn delete(&self, token: &str) -> Result<()> {
        let url = format!("{}/api/u/me", self.base);
        self.http
            .delete(url)
            .bearer_auth(token)
            .send()?
            .error_for_status()?;
        Ok(())
    }

    pub fn github_start(&self, handle: Option<&str>) -> Result<GithubStartResponse> {
        let url = format!("{}/api/auth/github/start", self.base);
        let body = serde_json::json!({ "handle": handle });
        let resp = self.http.post(url).json(&body).send()?.error_for_status()?;
        Ok(resp.json()?)
    }

    pub fn github_complete(&self, req: &GithubCompleteRequest<'_>) -> Result<GithubPoll> {
        let url = format!("{}/api/auth/github/complete", self.base);
        let resp = self.http.post(url).json(req).send()?;
        match resp.status().as_u16() {
            200 => Ok(GithubPoll::Done(resp.json::<TokenResponse>()?)),
            202 => {
                #[derive(Deserialize, Default)]
                struct Pending {
                    #[serde(default)]
                    slow_down: bool,
                }
                let p: Pending = resp.json().unwrap_or_default();
                Ok(GithubPoll::Pending { slow_down: p.slow_down })
            }
            status => {
                let body = resp.text().unwrap_or_default();
                anyhow::bail!("github auth failed ({status}): {body}")
            }
        }
    }
}

#[derive(Debug, Deserialize)]
pub struct GithubStartResponse {
    pub device_code: String,
    pub user_code: String,
    pub verification_uri: String,
    #[serde(default)]
    pub expires_in: u32,
    #[serde(default = "default_interval")]
    pub interval: u32,
}

fn default_interval() -> u32 { 5 }

#[derive(Debug, Serialize)]
pub struct GithubCompleteRequest<'a> {
    pub device_code: &'a str,
    pub handle: Option<&'a str>,
    pub text: Option<&'a str>,
    pub avatar_ascii: Option<&'a str>,
    pub metadata: Option<&'a BTreeMap<String, String>>,
}

pub enum GithubPoll {
    Done(TokenResponse),
    Pending { slow_down: bool },
}
