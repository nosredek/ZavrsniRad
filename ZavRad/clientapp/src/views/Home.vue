<template>
  <div class="container">
    <div class="centerPseudo">
      <div class="field">
        <div class="file is-info has-name">
          <label class="file-label">
            <input class="file-input" type="file" ref="fastaFile" @change="fastaFileChanged" />
            <span class="file-cta" style="width:130px;">
              <span class="file-icon">
                <font-awesome-icon icon="upload" />
              </span>
              <span class="file-label">FASTA file</span>
            </span>
            <span class="file-name">{{fastaFileName ? fastaFileName : "No file uploaded"}}</span>
          </label>
        </div>
      </div>

      <div class="field">
        <div class="file is-info has-name">
          <label class="file-label">
            <input class="file-input" type="file" ref="samFile" @change="samFileChanged" />
            <span class="file-cta" style="width:130px;">
              <span class="file-icon">
                <font-awesome-icon icon="upload" />
              </span>
              <span class="file-label">SAM file</span>
            </span>
            <span class="file-name">{{samFileName ? samFileName : "No file uploaded"}}</span>
          </label>
        </div>
      </div>

      <button @click="open">Open</button>

      <div v-if="isloading">
        <template v-for="(message,index) in messages">
          <p :key="index">{{message}}</p>
        </template>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator";

const { ipcRenderer } = require("electron");

import VueKonva from "vue-konva";
import Konva from "konva";
import axios from "axios";

import { SamFile } from "@/types/bioTypes";
import { FastLayer } from "konva/types/FastLayer";
import { Rect } from "konva/types/shapes/Rect";
import { ReferenceModel } from '@/store/types';

Vue.use(VueKonva);

@Component({
  components: {}
})
export default class Home extends Vue {
  samFileName: string = "";
  fastaFileName: string = "";

  isloading = false;
  messages: string[] = [];

  samFileChanged(): void {
    let f: any = this.$refs.samFile;
    this.samFileName = f.files[0].name;
  }

  fastaFileChanged(): void {
    let f: any = this.$refs.fastaFile;
    this.fastaFileName = f.files[0].name;
  }

  async open(): Promise<void> {
    let fasta: any = this.$refs.fastaFile;
    let sam: any = this.$refs.samFile;

    this.messages = ["Parsing and splitting files..."];
    this.isloading = true;

    await axios
      .post(`/main/init`, { alignmentFile: sam.files[0].path, referenceFile: fasta.files[0].path })
      .then((result: any) => {
        let references: Array<ReferenceModel> = result.data;
        this.$store.dispatch("setReferences", references);
      });
    this.messages[0] += " Done";
    this.messages.push("Sorting files...");
    await axios.get("/main/sortFiles");
    this.messages[1] += " Done";
    this.$router.push({ name: "visualization" });
  }
}
</script>

<style>
.centerPseudo {
  display: inline-block;
  text-align: center;
}

.centerPseudo::before {
  content: "";
  display: inline-block;
  height: 100%;
  vertical-align: middle;
  width: 0px;
}
</style>