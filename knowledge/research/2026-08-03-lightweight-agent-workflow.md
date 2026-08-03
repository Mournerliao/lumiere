# 轻量 Agent 工程运行框架研究

- 日期：2026-08-03
- 范围：面向 Lumiere 的 context engineering、harness engineering、持久状态、任务控制面与验证闭环
- 来源标准：原作者 X 帖、官方工程文章、论文及原始仓库

## 结论摘要

先进实践的共同方向不是把更多流程塞进模型上下文，而是建立一个薄的、仓库可见的控制面：给 agent 一张小地图，按任务渐进加载上下文，把任务状态放进 Issue tracker，把正确性尽量编码成确定性测试和约束，并让证据而非自我声明决定完成。

对 Lumiere 最合适的模型是风险自适应的 **Contract → Frontier → Evidence**：

1. Contract 定义不可漂移的产品、架构与声明边界。
2. Frontier 由一个无阻塞的 GitHub Issue 表示，不在多份 Markdown 中重复任务状态。
3. Evidence 区分仓库验证、Windows 验证和真实 HDR 硬件证据。
4. Git 与 ADR 保存历史和决策；短 `CURRENT.md` 只保存一屏内的当前态势。
5. Planner、evaluator、fresh-context handoff 和 Ralph loop 仅按风险升级，不作为每个任务的默认仪式。

## 第一手来源与事实

### 小入口与渐进披露

OpenAI 的 Harness Engineering 实践指出，一份巨大的 `AGENTS.md` 会迅速失效；其经验是给 agent “地图而不是一千页说明书”，从小而稳定的入口渐进披露知识，并用 CI 和 doc gardening 对抗陈旧文档。

- [OpenAI — Harness engineering: leveraging Codex in an agent-first world](https://openai.com/index/harness-engineering/)

2026 年对 repository context files 的实证研究发现，自动生成的说明文件没有稳定提高任务成功率，却平均增加约 20%–23% 的成本；agent 会认真遵守其中要求，因此冗余要求本身会制造额外工作。论文建议人工说明只保存最小必要要求。

- [Gloaguen et al. — Evaluating AGENTS.md](https://arxiv.org/abs/2602.11988)
- [Khatri — Do Context Files Help Coding Agents?](https://arxiv.org/abs/2607.27250)
- [Configuration Smells in AGENTS.md Files](https://arxiv.org/abs/2606.15828)

Jerry Liu 在 X 上总结“Files are all you need”：文件系统适合按需存储与搜索 context，也适合作为能力和工具的渐进披露接口，而非把所有信息预先注入 prompt。

- [Jerry Liu on X — Files are all you need](https://x.com/jerryjliu0/status/2011849758944690625)

### 任务控制面与持久状态

OpenAI Symphony 将项目管理 board 作为 agent 控制面，并把团队自己的运行策略版本化在仓库内的 `WORKFLOW.md`。它明确不试图成为通用工作流引擎；Issue tracker、运行策略、workspace 和 observability 保持分层。

- [OpenAI — An open-source spec for Codex orchestration: Symphony](https://openai.com/index/open-source-codex-orchestration-symphony/)

Anthropic 的长周期实验使用 feature list、进度文件和 Git 历史跨 context 交接，每次只推进一个功能，并要求会话结束时留下可合并的干净状态。关键不是保留全部对话，而是留下下一会话可快速恢复的结构化 artifact。

- [Anthropic — Effective harnesses for long-running agents](https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents)

### 复杂度按风险升级

Anthropic 后续 harness 研究采用 planner、generator、evaluator，但也记录：模型能力提升后，context reset 和 evaluator 对可靠范围内的任务会变成不必要开销；独立 evaluator 只在任务逼近模型可靠性边界时持续产生价值。

- [Anthropic — Harness design for long-running application development](https://www.anthropic.com/engineering/harness-design-long-running-apps)

宝玉在 X 上以工作上下文为标准区分 Skill 和 SubAgent：需要共享中间推理时使用 Skill；复杂且可独立交付的工作交给 SubAgent，并用“短摘要 + 详细文件指针”交接，避免主上下文污染。

- [宝玉 on X — Skill 与 SubAgent 的上下文取舍](https://x.com/dotey/status/2003712630582612066)

Aaron Levie 在 X 上强调 harness 必须对既有脚手架保持无情：模型能力变化后，过去的缓解层可能从帮助变成约束，应持续删除已经失去边际价值的机制。

- [Aaron Levie thread on X](https://x.com/dharmesh/status/2040085435821543459)

### 循环、验证与熵控制

Ralph 的有效内核很小：循环、fresh context、磁盘状态和可机检停止条件。原始模式及实现都强调用 Git/文件作为记忆；无限循环、模糊完成承诺或允许自动部署会放大风险。

- [Anthropic Claude Code — Ralph Wiggum plugin](https://github.com/anthropics/claude-code/blob/main/plugins/ralph-wiggum/README.md)
- [snarktank/ralph](https://github.com/snarktank/ralph)
- [Geoffrey Huntley on X — self-healing Ralph loop](https://x.com/GeoffreyHuntley/status/2012708172491030589)

OpenAI 把持续的小规模清理视作 garbage collection：agent 会复制仓库已有模式，坏模式也会扩散，因此应把重要边界转成确定性 lint、结构测试和日常小清理，而不是周期性进行昂贵的大扫除。

- [OpenAI — Harness engineering, Entropy and garbage collection](https://openai.com/index/harness-engineering/)

## 对 Lumiere 的适用原则

### 信息单一归属

| 信息 | 唯一权威位置 |
| --- | --- |
| 产品与架构不变量 | `knowledge/contracts/` |
| 重要选择及原因 | `knowledge/decisions/` |
| 当前阶段、frontier、阻塞 | `knowledge/state/CURRENT.md` |
| 具体目标、验收条件、依赖 | GitHub Issue |
| 操作方法 | `knowledge/runbooks/` |
| Windows/HDR 验证结果 | `knowledge/evidence/` |
| 变更历史 | Git |

`CURRENT.md` 不保存历史；Issue 不复制完整 contract；日志不重复 Git 和 Issue 已经拥有的信息。

### 三层完成语义

1. **Repository done**：相关代码、平台中立测试、格式和静态约束通过。
2. **Windows verified**：Windows restore/build/test/runtime smoke 通过。
3. **Hardware evidenced**：WGC、DXGI、HDR 显示目标、视觉匹配和接收应用行为有真实记录。

任何公开 HDR 声明必须达到第三层。前两层不能被写成“HDR 已支持”。

### 风险阶梯

- Level 0：微小、局部、可逆改动；直接实现并跑定向验证。
- Level 1：普通功能或修复；一个 Issue、明确验收条件、薄计划、相关 gate。
- Level 2：跨模块、平台生命周期、输出语义或公开声明；先研究/规格，必要时 ADR，完成后 fresh-context review。
- Level 3：跨会话长期任务；拆成有依赖的纵向 Issue，结构化 handoff，一次只推进一个 frontier。
- Level 4：满足机器可验证、边界明确、有迭代上限时，才启用 Ralph 或自动编排；禁止用于主观 HDR 实机判定和无人工审批的部署。

## 应避免的反模式

- 自动生成百科式 `AGENTS.md`，或要求每个任务先读全部知识库。
- 同时维护 roadmap checkbox、backlog、progress、loop log 和 Issue 五份任务状态。
- 把聊天 transcript 或流水账当作 durable state。
- 所有任务强制 planner、reviewer、evaluator 和多 agent。
- 为只运行过测试的功能填写实机验证结论。
- 无迭代上限、无机器停止条件的 Ralph loop。
- 为今天模型的暂时弱点建立难以删除的永久框架。

## 设计建议

先使用原生 Codex/GitHub/Git/CI 能力，不建立自定义 orchestrator。把精力优先投入：统一 Windows 验证入口、真实 HDR evidence 模板、依赖边界测试和短小的知识索引。只有观测到重复失败模式后，才把对应经验升级成 Skill、lint 或自动化。
