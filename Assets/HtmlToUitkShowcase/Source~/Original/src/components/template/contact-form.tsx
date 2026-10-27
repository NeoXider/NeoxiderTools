"use client";

import { useState } from "react";
import { CheckIcon, SendIcon } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";

/**
 * Frontend-only demo form — swap the submit handler for your API route
 * or form service when you ship.
 */
export function ContactForm() {
  const [sent, setSent] = useState(false);

  if (sent) {
    return (
      <div className="flex h-full min-h-72 flex-col items-center justify-center rounded-2xl border bg-card p-8 text-center">
        <span className="flex size-12 items-center justify-center rounded-full bg-primary/15 text-primary">
          <CheckIcon className="size-6" />
        </span>
        <h2 className="mt-4 text-lg font-semibold">Message sent</h2>
        <p className="mt-2 max-w-xs text-sm text-muted-foreground">
          Thanks for reaching out — we&apos;ll get back to you within one
          business day.
        </p>
        <Button
          variant="ghost"
          className="mt-6"
          onClick={() => setSent(false)}
        >
          Send another
        </Button>
      </div>
    );
  }

  return (
    <form
      className="rounded-2xl border bg-card p-8"
      onSubmit={(e) => {
        e.preventDefault();
        setSent(true);
      }}
    >
      <div className="grid gap-5 sm:grid-cols-2">
        <div className="grid gap-2">
          <Label htmlFor="name">Name</Label>
          <Input id="name" name="name" placeholder="Ada Lovelace" required />
        </div>
        <div className="grid gap-2">
          <Label htmlFor="email">Email</Label>
          <Input
            id="email"
            name="email"
            type="email"
            placeholder="ada@example.com"
            required
          />
        </div>
      </div>
      <div className="mt-5 grid gap-2">
        <Label htmlFor="message">Message</Label>
        <Textarea
          id="message"
          name="message"
          placeholder="Tell us what you're building…"
          className="min-h-36"
          required
        />
      </div>
      <Button type="submit" size="lg" className="mt-6 w-full rounded-full">
        <SendIcon className="size-4" />
        Send message
      </Button>
      <p className="mt-4 text-center text-xs text-muted-foreground">
        Demo form — no data leaves your browser.
      </p>
    </form>
  );
}
